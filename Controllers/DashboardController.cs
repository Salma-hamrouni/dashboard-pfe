using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DashboardAPI.Data;
using DashboardAPI.DTOs;
using DashboardAPI.Models;
using System.Text.Json;

namespace DashboardAPI.Controllers
{
    [Route("api/[controller]")]
    public class DashboardController : BaseController
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET api/dashboard ─────────────────────────────────────────────────
        /// <summary>Retourne uniquement les dashboards de l'utilisateur connecté.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();

            var dashboards = await _context.Dashboards
                .Where(d => d.UserId == userId)   // ✅ filtre par user
                .Include(d => d.Widgets)
                .ToListAsync();

            var result = dashboards.Select(d => ToDto(d));
            return Ok(result);
        }

        // ── GET api/dashboard/{id} ────────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();

            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dashboard == null) return NotFound();

            // ✅ Un user ne peut voir que ses propres dashboards (sauf si public)
            if (dashboard.UserId != userId && !dashboard.IsPublic)
                return Forbid();

            return Ok(ToDto(dashboard));
        }

        // ── POST api/dashboard ────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DashboardDetailDto dto)
        {
            var dashboard = new Dashboard
            {
                Name        = dto.Name,
                DatasetId   = dto.DatasetId,
                UserId      = GetUserId(),           // ✅ userId depuis le token

                ColumnsJson         = JsonSerializer.Serialize(dto.Columns         ?? new List<string>()),
                InsightsJson        = JsonSerializer.Serialize(dto.Insights        ?? new List<string>()),
                RecommendationsJson = JsonSerializer.Serialize(dto.Recommendations ?? new List<string>()),

                ShareToken = Guid.NewGuid().ToString(),
                IsPublic   = false,
                CreatedAt  = DateTime.UtcNow
            };

            _context.Dashboards.Add(dashboard);
            await _context.SaveChangesAsync();

            if (dto.Widgets != null && dto.Widgets.Any())
            {
                foreach (var w in dto.Widgets)
                {
                    w.DashboardId = dashboard.Id;
                    _context.Widgets.Add(w);
                }
                await _context.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = dashboard.Id }, ToDto(dashboard));
        }

        // ── PUT api/dashboard/{id} ────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DashboardDetailDto dto)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid(); // ✅ seul le proprio peut modifier

            dashboard.Name              = dto.Name;
            dashboard.DatasetId         = dto.DatasetId;
            dashboard.ColumnsJson       = JsonSerializer.Serialize(dto.Columns         ?? new List<string>());
            dashboard.InsightsJson      = JsonSerializer.Serialize(dto.Insights        ?? new List<string>());
            dashboard.RecommendationsJson = JsonSerializer.Serialize(dto.Recommendations ?? new List<string>());

            await _context.SaveChangesAsync();
            return Ok(ToDto(dashboard));
        }

        // ── DELETE api/dashboard/{id} ─────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid(); // ✅ seul le proprio peut supprimer

            _context.Dashboards.Remove(dashboard);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── GET api/dashboard/share/{token} ───────────────────────────────────
        /// <summary>Route publique — pas besoin d'être connecté.</summary>
        [HttpGet("share/{token}")]
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        public async Task<IActionResult> GetSharedDashboard(string token)
        {
            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.ShareToken == token);

            if (dashboard == null || !dashboard.IsPublic)
                return NotFound("Dashboard introuvable ou non public.");

            return Ok(ToDto(dashboard));
        }

        // ── PUT api/dashboard/share/{id} ──────────────────────────────────────
        [HttpPut("share/{id:int}")]
        public async Task<IActionResult> ToggleShare(int id, [FromQuery] bool isPublic)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(id);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid();

            dashboard.IsPublic = isPublic;
            await _context.SaveChangesAsync();

            return Ok(new { dashboard.ShareToken, dashboard.IsPublic });
        }

        // ── Helper privé ──────────────────────────────────────────────────────
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
            }).ToList() ?? new List<Widget>()
        };
    }
}