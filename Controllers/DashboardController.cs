using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardAPI.Data;
using DashboardAPI.DTOs;
using DashboardAPI.DTOs.Dashboard;
using DashboardAPI.Models;
using DashboardAPI.Common;
using DashboardAPI.Services;
using System.Text.Json;

namespace DashboardAPI.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController(AppDbContext context, AuditService audit) : BaseController
    {
        // ── GET api/dashboard ─────────────────────────────────────────────────
        /// <summary>Retourne les dashboards de l'utilisateur connecté, paginés et filtrables.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<DashboardResponseDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 10,
            [FromQuery] string? search   = null)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var userId = GetUserId();

            var query = context.Dashboards
                .AsNoTracking()
                .Where(d => d.UserId == userId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Name.Contains(search));

            var total = await query.CountAsync();

            var raw = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IsPublic,
                    d.ShareToken,
                    d.CreatedAt,
                    d.ColumnsJson,
                    WidgetCount = d.Widgets.Count
                })
                .ToListAsync();

            var dashboards = raw.Select(d => new DashboardResponseDto
            {
                Id          = d.Id,
                Name        = d.Name,
                IsPublic    = d.IsPublic,
                ShareToken  = d.IsPublic ? d.ShareToken : null,
                CreatedAt   = d.CreatedAt,
                WidgetCount = d.WidgetCount,
                Columns         = Deserialize(d.ColumnsJson),
                Insights        = [],
                Recommendations = [],
                Widgets         = []
            }).ToList();

            return Ok(ApiResponse<PagedResponse<DashboardResponseDto>>.Ok(new PagedResponse<DashboardResponseDto>
            {
                Items      = dashboards,
                TotalCount = total,
                Page       = page,
                PageSize   = pageSize
            }));
        }

        // ── GET api/dashboard/{id} ────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var dashboard = await context.Dashboards
                .AsNoTracking()
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dashboard == null) return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));

            if (dashboard.UserId != userId && !dashboard.IsPublic)
                return Forbid();

            return Ok(ApiResponse<DashboardDetailDto>.Ok(ToDto(dashboard)));
        }

        // ── POST api/dashboard ────────────────────────────────────────────────
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), 201)]
        public async Task<IActionResult> Create([FromBody] DashboardDetailDto dto)
        {
            var userId = GetUserId();

            var dashboard = new Dashboard
            {
                Name        = dto.Name,
                DatasetId   = dto.DatasetId ?? 0,
                UserId      = userId,
                ColumnsJson         = JsonSerializer.Serialize(dto.Columns         ?? []),
                InsightsJson        = JsonSerializer.Serialize(dto.Insights        ?? []),
                RecommendationsJson = JsonSerializer.Serialize(dto.Recommendations ?? []),
                ShareToken = Guid.NewGuid().ToString(),
                IsPublic   = false,
                CreatedAt  = DateTime.UtcNow
            };

            context.Dashboards.Add(dashboard);
            await context.SaveChangesAsync();

            if (dto.Widgets != null && dto.Widgets.Count > 0)
            {
                foreach (var w in dto.Widgets)
                {
                    w.DashboardId = dashboard.Id;
                    context.Widgets.Add(w);
                }
                await context.SaveChangesAsync();
            }

            await audit.LogAsync(userId, "DASHBOARD_CREATED", "Dashboard", dashboard.Id,
                $"name={dashboard.Name}", GetClientIp());

            return CreatedAtAction(nameof(GetById), new { id = dashboard.Id },
                ApiResponse<DashboardDetailDto>.Ok(ToDto(dashboard)));
        }

        // ── PUT api/dashboard/{id} ────────────────────────────────────────────
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, [FromBody] DashboardDetailDto dto)
        {
            var userId    = GetUserId();
            var dashboard = await context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));
            if (dashboard.UserId != userId) return Forbid();

            dashboard.Name                = dto.Name;
            dashboard.DatasetId           = dto.DatasetId ?? dashboard.DatasetId;
            dashboard.ColumnsJson         = JsonSerializer.Serialize(dto.Columns         ?? []);
            dashboard.InsightsJson        = JsonSerializer.Serialize(dto.Insights        ?? []);
            dashboard.RecommendationsJson = JsonSerializer.Serialize(dto.Recommendations ?? []);

            await context.SaveChangesAsync();

            await audit.LogAsync(userId, "DASHBOARD_UPDATED", "Dashboard", id,
                $"name={dto.Name}", GetClientIp());

            return Ok(ApiResponse<DashboardDetailDto>.Ok(ToDto(dashboard)));
        }

        // ── DELETE api/dashboard/{id} ─────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId    = GetUserId();
            var dashboard = await context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));
            if (dashboard.UserId != userId) return Forbid();

            context.Dashboards.Remove(dashboard);
            await context.SaveChangesAsync();

            await audit.LogAsync(userId, "DASHBOARD_DELETED", "Dashboard", id,
                $"name={dashboard.Name}", GetClientIp());

            return NoContent();
        }

        // ── GET api/dashboard/public ──────────────────────────────────────────
        /// <summary>Retourne tous les dashboards marqués IsPublic=true (lecture pour Viewer).</summary>
        [HttpGet("public")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<DashboardResponseDto>>), 200)]
        public async Task<IActionResult> GetPublic(
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 50,
            [FromQuery] string? search   = null)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = context.Dashboards
                .AsNoTracking()
                .Include(d => d.Widgets)
                .Where(d => d.IsPublic);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(d => d.Name.Contains(search));

            var total = await query.CountAsync();

            var raw = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.IsPublic,
                    d.ShareToken,
                    d.CreatedAt,
                    d.ColumnsJson,
                    WidgetCount = d.Widgets.Count,
                    OwnerName  = context.Users
                        .Where(u => u.Id == d.UserId)
                        .Select(u => u.Name ?? u.Email)
                        .FirstOrDefault(),
                    OwnerEmail = context.Users
                        .Where(u => u.Id == d.UserId)
                        .Select(u => u.Email)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var dashboards = raw.Select(d => new DashboardResponseDto
            {
                Id          = d.Id,
                Name        = d.Name,
                IsPublic    = d.IsPublic,
                ShareToken  = d.ShareToken,
                CreatedAt   = d.CreatedAt,
                WidgetCount = d.WidgetCount,
                OwnerName   = d.OwnerName,
                OwnerEmail  = d.OwnerEmail,
                Columns         = Deserialize(d.ColumnsJson),
                Insights        = [],
                Recommendations = [],
                Widgets         = []
            }).ToList();

            return Ok(ApiResponse<PagedResponse<DashboardResponseDto>>.Ok(new PagedResponse<DashboardResponseDto>
            {
                Items      = dashboards,
                TotalCount = total,
                Page       = page,
                PageSize   = pageSize
            }));
        }

        // ── GET api/dashboard/share/{token} ───────────────────────────────────
        /// <summary>Route publique — pas besoin d'être connecté.</summary>
        [HttpGet("share/{token}")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<DashboardDetailDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSharedDashboard(string token)
        {
            var dashboard = await context.Dashboards
                .AsNoTracking()
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.ShareToken == token);

            if (dashboard == null || !dashboard.IsPublic)
                return NotFound(ApiResponse<object>.Fail("Dashboard introuvable ou non public."));

            return Ok(ApiResponse<DashboardDetailDto>.Ok(ToDto(dashboard)));
        }

        // ── PUT api/dashboard/share/{id} ──────────────────────────────────────
        [HttpPut("share/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> ToggleShare(int id, [FromQuery] bool isPublic)
        {
            var userId    = GetUserId();
            var dashboard = await context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));
            if (dashboard.UserId != userId) return Forbid();

            dashboard.IsPublic = isPublic;
            await context.SaveChangesAsync();

            await audit.LogAsync(userId, "DASHBOARD_SHARE_TOGGLED", "Dashboard", id,
                $"isPublic={isPublic}", GetClientIp());

            return Ok(ApiResponse<object>.Ok(new
            {
                dashboard.ShareToken,
                dashboard.IsPublic
            }));
        }

        // ── Helpers privés ────────────────────────────────────────────────────
        private static List<string> Deserialize(string? json) =>
            string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];

        private static DashboardDetailDto ToDto(Dashboard d) => new()
        {
            Id              = d.Id,
            Name            = d.Name,
            DatasetId       = d.DatasetId,
            Columns         = JsonSerializer.Deserialize<List<string>>(d.ColumnsJson         ?? "[]"),
            Insights        = JsonSerializer.Deserialize<List<string>>(d.InsightsJson        ?? "[]"),
            Recommendations = JsonSerializer.Deserialize<List<string>>(d.RecommendationsJson ?? "[]"),
            Widgets         = d.Widgets?.Select(w => new Widget
            {
                Id          = w.Id,
                Type        = w.Type,
                Title       = w.Title,
                Data        = w.Data,
                DashboardId = w.DashboardId
            }).ToList() ?? []
        };
    }
}
