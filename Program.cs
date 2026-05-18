using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using DashboardAPI.Data;
using DashboardAPI.Middlewares;
using DashboardAPI.Services;

// ── Serilog bootstrap (avant tout le reste) ───────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    Log.Information("=== Dashboard Generator API — démarrage ===");

    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog intégration complète ──────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        var logPath = Path.Combine("logs", "api-.log");

        config
            .ReadFrom.Configuration(ctx.Configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            // Console lisible en dev
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            // Fichier rolling journalier — tous les niveaux
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] " +
                                "{Message:lj} {Properties}{NewLine}{Exception}")
            // Fichier séparé pour les erreurs uniquement
            .WriteTo.File(
                Path.Combine("logs", "errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90);
    });

    // ── Controllers + validation automatique des DTOs ─────────────────────────
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(opt =>
        {
            opt.InvalidModelStateResponseFactory = ctx =>
            {
                var errors = ctx.ModelState
                    .Where(e => e.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());
                return new BadRequestObjectResult(new { success = false, errors });
            };
        });

    builder.Services.AddEndpointsApiExplorer();

    // ── Cache mémoire ─────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();

    // ── Rate Limiting ─────────────────────────────────────────────────────────
    var authWindow = builder.Configuration.GetValue("RateLimit:AuthWindowSeconds", 60);
    var authPermit = builder.Configuration.GetValue("RateLimit:AuthPermitLimit", 5);

    builder.Services.AddRateLimiter(opt =>
    {
        opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Login / register — anti brute-force (5 req/min par IP)
        opt.AddFixedWindowLimiter("auth-policy", o =>
        {
            o.Window      = TimeSpan.FromSeconds(authWindow);
            o.PermitLimit = authPermit;
            o.QueueLimit  = 0;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        // Endpoints IA — Gemini coûte cher (15 req/min)
        opt.AddFixedWindowLimiter("ai-policy", o =>
        {
            o.Window      = TimeSpan.FromSeconds(60);
            o.PermitLimit = 15;
            o.QueueLimit  = 0;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        // Upload CSV/fichiers — opération lourde (5 req/min)
        opt.AddFixedWindowLimiter("upload-policy", o =>
        {
            o.Window      = TimeSpan.FromSeconds(60);
            o.PermitLimit = 5;
            o.QueueLimit  = 0;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        // Export JSON/PDF — génération coûteuse (20 req/min)
        opt.AddFixedWindowLimiter("export-policy", o =>
        {
            o.Window      = TimeSpan.FromSeconds(60);
            o.PermitLimit = 20;
            o.QueueLimit  = 0;
            o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    });

    // ── Swagger ───────────────────────────────────────────────────────────────
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title   = "Dashboard Generator API",
            Version = "v1",
            Description = """
                API backend du générateur de tableaux de bord interactifs.

                ## Authentification
                Utilisez **POST /api/auth/login** pour obtenir un token JWT,
                puis cliquez sur **Authorize** et entrez votre token.

                ## Modules
                - **Auth** — Inscription, connexion, gestion des rôles
                - **Dashboard** — CRUD + pagination + recherche
                - **Widget** — CRUD + données dynamiques (agrégation SUM/AVG/COUNT)
                - **DataSource** — Connecteurs CSV, MySQL, SQL Server, REST
                - **Dataset** — Upload et analyse de données
                - **AI** — Recommandations, KPIs, analyse, chat conversationnel
                - **Export** — Export JSON et PDF
                - **DashboardShare** — Partage par lien avec permissions
                - **DashboardVersion** — Historique et restauration
                - **Stats** — Statistiques système (Admin)
                - **Health** — Monitoring santé API

                """,
            Contact = new OpenApiContact { Name = "Dashboard Generator PFE" }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "Bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Format : Bearer {votre-token-jwt}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });

        c.OrderActionsBy(a => a.GroupName);
    });

    // ── CORS ──────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                    "http://localhost:5144",
                    "http://localhost:3000",
                    "http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials());
    });

    // ── Base de données ───────────────────────────────────────────────────────
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            ServerVersion.AutoDetect(
                builder.Configuration.GetConnectionString("DefaultConnection"))
        )
    );

    // ── Authentication JWT ────────────────────────────────────────────────────
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Jwt:Issuer"],
                ValidAudience            = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey         = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("JWT auth failed: {Error}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // ── DAOs ──────────────────────────────────────────────────────────────────
    builder.Services.AddScoped<DashboardDao>();
    builder.Services.AddScoped<WidgetDao>();
    builder.Services.AddScoped<DataSourceDao>();

    // ── Services métier ───────────────────────────────────────────────────────
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<CsvParserService>();
    builder.Services.AddScoped<SqlConnectorService>();
    builder.Services.AddScoped<SqlServerConnectorService>();
    builder.Services.AddScoped<WidgetDataService>();
    builder.Services.AddScoped<AuditService>();               // ← Audit logs

    builder.Services.AddHttpClient<AiService>();
    builder.Services.AddHttpClient<RestConnectorService>();
    builder.Services.AddHttpClient();

    // ── Background services ───────────────────────────────────────────────────
    builder.Services.AddHostedService<DataSourceRefreshService>(); // ← Auto-refresh

    // ── Upload limits ─────────────────────────────────────────────────────────
    builder.Services.Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 20 * 1024 * 1024; // 20 MB
    });

    // ─────────────────────────────────────────────────────────────────────────
    var app = builder.Build();
    // ─────────────────────────────────────────────────────────────────────────

    // Créer dossiers au démarrage
    Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "Uploads", "csv"));
    Directory.CreateDirectory("logs");

    // Migrations automatiques
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Log.Information("Migrations appliquées avec succès");
    }

    // ── Pipeline middleware (ordre critique) ──────────────────────────────────

    // 1. Exception handler global — PREMIER pour tout attraper
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 2. Security headers
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // 3. Serilog request logging (après exception handler)
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.000}ms)";
        opts.GetLevel = (ctx, elapsed, ex) =>
            ex != null || ctx.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : ctx.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;
    });

    // 4. Swagger
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard Generator API v1");
        c.RoutePrefix       = "swagger";
        c.DocumentTitle     = "Dashboard API";
        c.DefaultModelsExpandDepth(-1);
        c.DisplayRequestDuration();
    });

    // 5. CORS avant auth
    app.UseCors("AllowFrontend");

    // 6. Rate limiting
    app.UseRateLimiter();

    // 7. Auth
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    Log.Information("API démarrée — Swagger: /swagger");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Démarrage API échoué");
}
finally
{
    Log.CloseAndFlush();
}
