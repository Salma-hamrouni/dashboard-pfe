using System.Net;
using System.Text.Json;
using DashboardAPI.Common;

namespace DashboardAPI.Middlewares
{
    public class GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var traceId = context.TraceIdentifier;

            logger.LogError(ex,
                "Unhandled exception [{TraceId}] {Method} {Path} — {Message}",
                traceId,
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            var (statusCode, message) = ex switch
            {
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized,  "Accès non autorisé."),
                KeyNotFoundException        => (HttpStatusCode.NotFound,      "Ressource introuvable."),
                ArgumentException           => (HttpStatusCode.BadRequest,    ex.Message),
                InvalidOperationException   => (HttpStatusCode.BadRequest,    ex.Message),
                _                           => (HttpStatusCode.InternalServerError, "Une erreur interne est survenue.")
            };

            context.Response.StatusCode  = (int)statusCode;
            context.Response.ContentType = "application/json";

            var response = ApiResponse<object>.Fail(message);

            // En développement, inclure le détail technique
            if (env.IsDevelopment())
            {
                response = new()
                {
                    Success = false,
                    Error   = $"{message} | {ex.GetType().Name}: {ex.Message}",
                    Meta    = new { traceId }
                };
            }

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            );
        }
    }
}
