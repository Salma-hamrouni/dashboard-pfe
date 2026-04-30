using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using DashboardAPI.Data;
using DashboardAPI.Models;

namespace DashboardAPI.Controllers
{
    [Route("api/dashboard-version")]
    public class DashboardVersionController : BaseController
    {
        private readonly AppDbContext _context;

        public DashboardVersionController(AppDbContext context)
        {
            _context = context;
        }

        // ── POST api/dashboard-version/{dashboardId}/save ─────────────────────
        /// <summary>
        /// Crée un snapshot de la version actuelle du dashboard.
        /// Appelé automatiquement à chaque sauvegarde importante.
        /// </summary>
        [HttpPost("{dashboardId:int}/save")]
        public async Task<IActionResult> SaveVersion(
            int dashboardId,
            [FromBody] SaveVersionRequest request)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == dashboardId);

            if (dashboard == null) return NotFound("Dashboard introuvable.");
            if (dashboard.UserId != userId) return Forbid();

            // Numéro de version auto-incrémenté
            var lastVersion = await _context.DashboardVersions
                .Where(v => v.DashboardId == dashboardId)
                .MaxAsync(v => (int?)v.Version) ?? 0;

            var snapshot = new
            {
                dashboard = new
                {
                    dashboard.Id,
                    dashboard.Name,
                    dashboard.DatasetId,
                    dashboard.ColumnsJson,
                    dashboard.InsightsJson,
                    dashboard.RecommendationsJson
                },
                widgets = dashboard.Widgets.Select(w => new
                {
                    w.Id, w.Type, w.Title, w.Data, w.DashboardId
                })
            };

            var version = new DashboardVersion
            {
                DashboardId  = dashboardId,
                Version      = lastVersion + 1,
                Label        = request.Label,
                SnapshotJson = JsonSerializer.Serialize(snapshot),
                CreatedAt    = DateTime.UtcNow
            };

            _context.DashboardVersions.Add(version);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                version.Id,
                version.Version,
                version.Label,
                version.CreatedAt,
                widgetCount = dashboard.Widgets.Count
            });
        }

        // ── GET api/dashboard-version/{dashboardId} ───────────────────────────
        /// <summary>Liste toutes les versions d'un dashboard.</summary>
        [HttpGet("{dashboardId:int}")]
        public async Task<IActionResult> GetVersions(int dashboardId)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(dashboardId);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid();

            var versions = await _context.DashboardVersions
                .Where(v => v.DashboardId == dashboardId)
                .OrderByDescending(v => v.Version)
                .Select(v => new
                {
                    v.Id,
                    v.Version,
                    v.Label,
                    v.CreatedAt
                })
                .ToListAsync();

            return Ok(versions);
        }

        // ── GET api/dashboard-version/{dashboardId}/{versionNumber} ───────────
        /// <summary>Retourne le snapshot complet d'une version.</summary>
        [HttpGet("{dashboardId:int}/{versionNumber:int}")]
        public async Task<IActionResult> GetVersion(int dashboardId, int versionNumber)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(dashboardId);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid();

            var version = await _context.DashboardVersions
                .FirstOrDefaultAsync(v =>
                    v.DashboardId == dashboardId &&
                    v.Version == versionNumber);

            if (version == null) return NotFound($"Version {versionNumber} introuvable.");

            return Ok(new
            {
                version.Id,
                version.Version,
                version.Label,
                version.CreatedAt,
                snapshot = JsonSerializer.Deserialize<JsonElement>(version.SnapshotJson)
            });
        }

        // ── POST api/dashboard-version/{dashboardId}/{versionNumber}/restore ──
        /// <summary>
        /// Restaure une version précédente du dashboard.
        /// Crée automatiquement un snapshot de la version actuelle avant restauration.
        /// </summary>
        [HttpPost("{dashboardId:int}/{versionNumber:int}/restore")]
        public async Task<IActionResult> Restore(int dashboardId, int versionNumber)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == dashboardId);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid();

            var version = await _context.DashboardVersions
                .FirstOrDefaultAsync(v =>
                    v.DashboardId == dashboardId &&
                    v.Version == versionNumber);

            if (version == null) return NotFound($"Version {versionNumber} introuvable.");

            // Sauvegarder la version actuelle avant restauration
            var lastVersion = await _context.DashboardVersions
                .Where(v => v.DashboardId == dashboardId)
                .MaxAsync(v => (int?)v.Version) ?? 0;

            var backupSnapshot = new
            {
                dashboard = new
                {
                    dashboard.Id, dashboard.Name, dashboard.DatasetId,
                    dashboard.ColumnsJson, dashboard.InsightsJson, dashboard.RecommendationsJson
                },
                widgets = dashboard.Widgets.Select(w => new
                {
                    w.Id, w.Type, w.Title, w.Data, w.DashboardId
                })
            };

            _context.DashboardVersions.Add(new DashboardVersion
            {
                DashboardId  = dashboardId,
                Version      = lastVersion + 1,
                Label        = $"Backup avant restauration v{versionNumber}",
                SnapshotJson = JsonSerializer.Serialize(backupSnapshot),
                CreatedAt    = DateTime.UtcNow
            });

            // Restaurer depuis le snapshot
            var snapshot = JsonSerializer.Deserialize<JsonElement>(version.SnapshotJson);

            if (snapshot.TryGetProperty("dashboard", out var d))
            {
                if (d.TryGetProperty("name", out var name))
                    dashboard.Name = name.GetString() ?? dashboard.Name;
                if (d.TryGetProperty("columnsJson", out var cols))
                    dashboard.ColumnsJson = cols.GetString() ?? dashboard.ColumnsJson;
                if (d.TryGetProperty("insightsJson", out var ins))
                    dashboard.InsightsJson = ins.GetString() ?? dashboard.InsightsJson;
                if (d.TryGetProperty("recommendationsJson", out var rec))
                    dashboard.RecommendationsJson = rec.GetString() ?? dashboard.RecommendationsJson;
            }

            // Supprimer les widgets actuels et restaurer ceux du snapshot
            _context.Widgets.RemoveRange(dashboard.Widgets);

            if (snapshot.TryGetProperty("widgets", out var widgets))
            {
                foreach (var w in widgets.EnumerateArray())
                {
                    _context.Widgets.Add(new Widget
                    {
                        DashboardId = dashboardId,
                        Type        = w.TryGetProperty("type",  out var t)  ? t.GetString()  ?? "bar" : "bar",
                        Title       = w.TryGetProperty("title", out var ti) ? ti.GetString() ?? ""    : "",
                        Data        = w.TryGetProperty("data",  out var dt) ? dt.GetString() ?? "{}"  : "{}"
                    });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message          = $"Dashboard restauré à la version {versionNumber}.",
                restoredVersion  = versionNumber,
                backupVersion    = lastVersion + 1
            });
        }

        // ── DELETE api/dashboard-version/{id} ────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var version = await _context.DashboardVersions
                .Include(v => v.Dashboard)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (version == null) return NotFound();
            if (version.Dashboard?.UserId != GetUserId()) return Forbid();

            _context.DashboardVersions.Remove(version);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public record SaveVersionRequest(string? Label);
}