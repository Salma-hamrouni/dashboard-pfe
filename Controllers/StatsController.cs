using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using DashboardAPI.Data;
using DashboardAPI.Common;

namespace DashboardAPI.Controllers
{
    /// <summary>
    /// Statistiques système — réservé Admin.
    /// Utile pour la démo PFE : montre l'activité réelle de la plateforme.
    /// </summary>
    [Route("api/stats")]
    [Authorize(Roles = "Admin")]
    public class StatsController(AppDbContext context, IMemoryCache cache) : BaseController
    {
        private static readonly MemoryCacheEntryOptions _statsCacheOpts = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
            Size = 1
        };

        // ── GET /api/stats/overview ───────────────────────────────────────────
        /// <summary>Vue globale de la plateforme — mise en cache 5 minutes.</summary>
        [HttpGet("overview")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> Overview()
        {
            const string key = "stats:overview";
            if (cache.TryGetValue(key, out object? cached))
                return Ok(ApiResponse<object>.Ok(cached!, new { source = "cache" }));

            var now   = DateTime.UtcNow;
            var day   = now.AddDays(-1);
            var week  = now.AddDays(-7);
            var month = now.AddDays(-30);

            var totalUsers      = await context.Users.CountAsync();
            var totalDashboards = await context.Dashboards.CountAsync();
            var totalWidgets    = await context.Widgets.CountAsync();
            var totalDatasets   = await context.Datasets.CountAsync();
            var totalSources    = await context.DataSources.CountAsync();

            var activeShares = await context.DashboardShares
                .Where(s => s.ExpiresAt == null || s.ExpiresAt > now)
                .CountAsync();

            var publicDashboards = await context.Dashboards
                .Where(d => d.IsPublic)
                .CountAsync();

            var newUsersLast30d = await context.Users
                .Where(u => u.CreatedAt >= month)
                .CountAsync();

            var dashboardsLast7d = await context.Dashboards
                .Where(d => d.CreatedAt >= week)
                .CountAsync();

            var result = new
            {
                totals = new
                {
                    users       = totalUsers,
                    dashboards  = totalDashboards,
                    widgets     = totalWidgets,
                    datasets    = totalDatasets,
                    dataSources = totalSources
                },
                activity = new
                {
                    newUsersLast30Days         = newUsersLast30d,
                    dashboardsCreatedLast7Days = dashboardsLast7d,
                    activeShares               = activeShares,
                    publicDashboards           = publicDashboards
                },
                generatedAt = now
            };

            cache.Set(key, (object)result, _statsCacheOpts);
            return Ok(ApiResponse<object>.Ok(result, new { source = "db" }));
        }

        // ── GET /api/stats/users ──────────────────────────────────────────────
        /// <summary>Répartition des utilisateurs par rôle + activité.</summary>
        [HttpGet("users")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> UsersStats()
        {
            var byRole = await context.Users
                .GroupBy(u => u.Role)
                .Select(g => new { role = g.Key, count = g.Count() })
                .ToListAsync();

            var recentUsers = await context.Users
                .OrderByDescending(u => u.CreatedAt)
                .Take(10)
                .Select(u => new { u.Id, u.Email, u.Role, u.CreatedAt })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { byRole, recentUsers }));
        }

        // ── GET /api/stats/dashboards ─────────────────────────────────────────
        /// <summary>Top dashboards par nombre de widgets + dashboards récents.</summary>
        [HttpGet("dashboards")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DashboardsStats()
        {
            var topByWidgets = await context.Dashboards
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IsPublic,
                    d.CreatedAt,
                    WidgetCount = d.Widgets.Count
                })
                .OrderByDescending(d => d.WidgetCount)
                .Take(10)
                .ToListAsync();

            var recentVersions = await context.DashboardVersions
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .Select(v => new { v.DashboardId, v.Version, v.Label, v.CreatedAt })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { topByWidgets, recentVersions }));
        }

        // ── GET /api/stats/datasources ────────────────────────────────────────
        /// <summary>Répartition des sources de données par type.</summary>
        [HttpGet("datasources")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> DataSourcesStats()
        {
            var byType = await context.DataSources
                .GroupBy(ds => ds.Type)
                .Select(g => new { type = g.Key.ToString(), count = g.Count() })
                .ToListAsync();

            var staleSources = await context.DataSources
                .Where(ds => ds.LastRefreshedAt < DateTime.UtcNow.AddHours(-24))
                .Select(ds => new { ds.Id, ds.Name, ds.Type, ds.LastRefreshedAt })
                .ToListAsync();

            return Ok(ApiResponse<object>.Ok(new { byType, staleSources }));
        }

        // ── GET /api/stats/audit ──────────────────────────────────────────────
        /// <summary>Activité d'audit : actions les plus fréquentes, utilisateurs les plus actifs.</summary>
        [HttpGet("audit")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> AuditStats()
        {
            var week = DateTime.UtcNow.AddDays(-7);

            var topActions = await context.AuditLogs
                .Where(a => a.CreatedAt >= week)
                .GroupBy(a => a.Action)
                .Select(g => new { action = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            var mostActiveUsers = await context.AuditLogs
                .Where(a => a.CreatedAt >= week)
                .GroupBy(a => a.UserId)
                .Select(g => new { userId = g.Key, actions = g.Count() })
                .OrderByDescending(x => x.actions)
                .Take(10)
                .ToListAsync();

            var recentLogs = await context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(20)
                .Select(a => new
                {
                    a.Id,
                    a.UserId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.IpAddress,
                    a.CreatedAt
                })
                .ToListAsync();

            var totalLast7d = await context.AuditLogs
                .CountAsync(a => a.CreatedAt >= week);

            return Ok(ApiResponse<object>.Ok(new
            {
                totalActionsLast7Days = totalLast7d,
                topActions,
                mostActiveUsers,
                recentLogs
            }));
        }

        // ── GET /api/stats/widgets ────────────────────────────────────────────
        /// <summary>Popularité des types de widgets et widgets les plus consultés.</summary>
        [HttpGet("widgets")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> WidgetsStats()
        {
            var byType = await context.Widgets
                .GroupBy(w => w.Type)
                .Select(g => new { type = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .ToListAsync();

            var dashboardsWithMostWidgets = await context.Dashboards
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.UserId,
                    WidgetCount = d.Widgets.Count,
                    d.CreatedAt
                })
                .Where(d => d.WidgetCount > 0)
                .OrderByDescending(d => d.WidgetCount)
                .Take(10)
                .ToListAsync();

            var widgetsWithDataSource = await context.Widgets
                .CountAsync(w => w.Config != null && w.Config.Contains("dataSourceId"));

            var totalWidgets = await context.Widgets.CountAsync();

            return Ok(ApiResponse<object>.Ok(new
            {
                byType,
                dashboardsWithMostWidgets,
                widgetsWithDataSource,
                widgetsWithoutDataSource = totalWidgets - widgetsWithDataSource,
                totalWidgets
            }));
        }
    }
}
