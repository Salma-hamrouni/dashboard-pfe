using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using DashboardAPI.Data;
using DashboardAPI.Common;

namespace DashboardAPI.Controllers
{
    /// <summary>
    /// Endpoints de santé — utilisés par les load balancers, Docker healthcheck et monitoring.
    /// GET /api/health        → liveness (répond toujours si l'API tourne)
    /// GET /api/health/ready  → readiness (DB + dépendances OK ?)
    /// GET /api/health/detail → détail complet (Admin uniquement)
    /// </summary>
    [ApiController]
    [Route("api/health")]
    public class HealthController(
        AppDbContext context,
        IConfiguration config,
        IMemoryCache cache,
        ILogger<HealthController> logger) : ControllerBase
    {
        // ── GET /api/health ── liveness ───────────────────────────────────────
        /// <summary>Liveness probe : l'API répond-elle ? (Kubernetes / Docker)</summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Live() =>
            Ok(new { status = "alive", timestamp = DateTime.UtcNow, version = "1.0" });

        // ── GET /api/health/ready ── readiness ────────────────────────────────
        /// <summary>Readiness probe : toutes les dépendances critiques sont-elles disponibles ?</summary>
        [HttpGet("ready")]
        [AllowAnonymous]
        public async Task<IActionResult> Ready()
        {
            var sw    = Stopwatch.StartNew();
            var dbOk  = await CheckDbAsync();
            sw.Stop();

            var status = dbOk ? "ready" : "degraded";
            var code   = dbOk ? 200 : 503;

            return StatusCode(code, new
            {
                status,
                timestamp   = DateTime.UtcNow,
                checks = new
                {
                    database = new { ok = dbOk, latencyMs = sw.ElapsedMilliseconds }
                }
            });
        }

        // ── GET /api/health/detail ── détail complet ──────────────────────────
        /// <summary>Détail complet de toutes les dépendances — réservé Admin.</summary>
        [HttpGet("detail")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Detail()
        {
            var checks = new Dictionary<string, object>();
            var allOk  = true;

            // ── DB ────────────────────────────────────────────────────────────
            var dbSw  = Stopwatch.StartNew();
            var dbOk  = await CheckDbAsync();
            dbSw.Stop();

            checks["database"] = new
            {
                ok        = dbOk,
                latencyMs = dbSw.ElapsedMilliseconds,
                status    = dbOk ? "connected" : "unreachable"
            };
            if (!dbOk) allOk = false;

            // ── DB stats ──────────────────────────────────────────────────────
            if (dbOk)
            {
                try
                {
                    var userCount      = await context.Users.CountAsync();
                    var dashboardCount = await context.Dashboards.CountAsync();
                    var widgetCount    = await context.Widgets.CountAsync();

                    checks["database_stats"] = new
                    {
                        users      = userCount,
                        dashboards = dashboardCount,
                        widgets    = widgetCount
                    };
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Health: DB stats query failed");
                    checks["database_stats"] = new { error = ex.Message };
                }
            }

            // ── Cache ─────────────────────────────────────────────────────────
            var cacheKey = "_health_probe_" + Guid.NewGuid();
            cache.Set(cacheKey, 1, TimeSpan.FromSeconds(5));
            var cacheOk = cache.TryGetValue(cacheKey, out _);
            cache.Remove(cacheKey);

            checks["cache"] = new { ok = cacheOk, type = "IMemoryCache" };
            if (!cacheOk) allOk = false;

            // ── Process ───────────────────────────────────────────────────────
            var proc = Process.GetCurrentProcess();
            checks["process"] = new
            {
                ok              = true,
                pid             = proc.Id,
                memoryMb        = Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1),
                threadCount     = proc.Threads.Count,
                uptimeSeconds   = (long)(DateTime.UtcNow - proc.StartTime.ToUniversalTime()).TotalSeconds
            };

            // ── Config ────────────────────────────────────────────────────────
            checks["config"] = new
            {
                ok      = true,
                jwt     = !string.IsNullOrEmpty(config["Jwt:Key"]),
                gemini  = !string.IsNullOrEmpty(config["Gemini:ApiKey"]),
                connStr = !string.IsNullOrEmpty(config.GetConnectionString("DefaultConnection"))
            };

            return StatusCode(allOk ? 200 : 503, ApiResponse<object>.Ok(new
            {
                status    = allOk ? "healthy" : "degraded",
                timestamp = DateTime.UtcNow,
                checks
            }));
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private async Task<bool> CheckDbAsync()
        {
            try   { return await context.Database.CanConnectAsync(); }
            catch { return false; }
        }
    }
}
