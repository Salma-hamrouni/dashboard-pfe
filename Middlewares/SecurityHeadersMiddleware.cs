namespace DashboardAPI.Middlewares
{
    /// <summary>
    /// Ajoute les headers de sécurité HTTP recommandés sur chaque réponse.
    /// Protège contre XSS, clickjacking, MIME sniffing et autres attaques courantes.
    /// </summary>
    public class SecurityHeadersMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var headers    = context.Response.Headers;
            var isSwagger  = context.Request.Path.StartsWithSegments("/swagger");

            headers["X-Frame-Options"]       = "DENY";
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-XSS-Protection"]      = "1; mode=block";
            headers["Referrer-Policy"]        = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"]     = "geolocation=(), microphone=(), camera=()";
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            // Swagger UI needs self-hosted scripts, styles, and inline execution.
            // All other routes are strict REST — no browser content expected.
            headers["Content-Security-Policy"] = isSwagger
                ? "default-src 'self'; " +
                  "script-src 'self' 'unsafe-inline'; " +
                  "style-src 'self' 'unsafe-inline'; " +
                  "img-src 'self' data:; " +
                  "connect-src 'self';"
                : "default-src 'none'; frame-ancestors 'none'";

            await next(context);
        }
    }
}
