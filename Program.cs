using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DashboardAPI.Data;
using DashboardAPI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger complet avec JWT + documentation ──────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Dashboard Generator API",
        Version     = "v1",
        Description = """
            API backend du générateur de tableaux de bord interactifs.

            ## Authentification
            Utilisez **POST /api/auth/login** pour obtenir un token JWT,
            puis cliquez sur **Authorize** et entrez votre token.

            ## Modules
            - **Auth** — Inscription, connexion, gestion des rôles
            - **Dashboard** — CRUD dashboards avec contrôle d'accès
            - **Widget** — Gestion des widgets par dashboard
            - **DataSource** — Connecteurs CSV, SQL, REST
            - **Dataset** — Upload et analyse de données
            - **AI** — Recommandations, KPIs, analyse, chat
            - **Export** — Export JSON et PDF
            - **DashboardShare** — Partage par lien avec permissions
            - **DashboardVersion** — Historique et restauration
            """,
        Contact = new OpenApiContact
        {
            Name  = "PFE NeoLedge",
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Entrez votre token JWT. Exemple : eyJhbGci..."
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

    // Ordonner les controllers par nom dans Swagger
    c.OrderActionsBy(a => a.GroupName);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);

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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── DAOs ──────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<DashboardDao>();
builder.Services.AddScoped<WidgetDao>();
builder.Services.AddScoped<DataSourceDao>();

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CsvParserService>();
builder.Services.AddScoped<SqlConnectorService>();
builder.Services.AddHttpClient<AiService>();
builder.Services.AddHttpClient<RestConnectorService>();
builder.Services.AddHttpClient();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 20 * 1024 * 1024;
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

// Créer le dossier uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "Uploads", "csv");
Directory.CreateDirectory(uploadsPath);

// Appliquer les migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ── Middleware ────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard Generator API v1");
        c.RoutePrefix      = "swagger";
        c.DocumentTitle    = "Dashboard API";
        c.DefaultModelsExpandDepth(-1); // cacher les schémas par défaut
    });
}
else
{
    // En production, Swagger reste accessible (utile pour la démo PFE)
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dashboard Generator API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();