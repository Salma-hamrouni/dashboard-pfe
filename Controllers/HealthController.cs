using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAPI.Data;

namespace DashboardAPI.Controllers
{
    /// <summary>
    /// Endpoints de santé et de statut de l'API.
    /// Remplace l'ancien AITestController.
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly AppDbContext   _context;
        private readonly IConfiguration _config;

        public HealthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config  = config;
        }

        // ── GET api/health ────────────────────────────────────────────────────
        /// <summary>Vérifie que l'API répond correctement.</summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            return Ok(new
            {
                status    = "healthy",
                timestamp = DateTime.UtcNow,
                version   = "1.0"
            });
        }

        // ── GET api/health/db ─────────────────────────────────────────────────
        /// <summary>Vérifie la connexion à la base de données.</summary>
        [HttpGet("db")]
        [AllowAnonymous]
        public IActionResult CheckDb()
        {
            try
            {
                var canConnect = _context.Database.CanConnect();
                return Ok(new
                {
                    database  = canConnect ? "connected" : "unreachable",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    database  = "error",
                    message   = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        // ── GET api/health/config ─────────────────────────────────────────────
        /// <summary>Vérifie que les clés de configuration essentielles sont présentes.</summary>
        [HttpGet("config")]
        [Authorize(Roles = "Admin")]
        public IActionResult CheckConfig()
        {
            return Ok(new
            {
                jwt = new
                {
                    key      = !string.IsNullOrEmpty(_config["Jwt:Key"])      ? "✅ présent" : "❌ manquant",
                    issuer   = !string.IsNullOrEmpty(_config["Jwt:Issuer"])   ? "✅ présent" : "❌ manquant",
                    audience = !string.IsNullOrEmpty(_config["Jwt:Audience"]) ? "✅ présent" : "❌ manquant"
                },
                gemini = new
                {
                    apiKey = !string.IsNullOrEmpty(_config["Gemini:ApiKey"]) ? "✅ présent" : "❌ manquant"
                },
                database = new
                {
                    connectionString = !string.IsNullOrEmpty(
                        _config.GetConnectionString("DefaultConnection")) ? "✅ présent" : "❌ manquant"
                }
            });
        }
    }
}
