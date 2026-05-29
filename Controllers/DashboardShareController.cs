using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using DashboardAPI.Data;
using DashboardAPI.Models;

namespace DashboardAPI.Controllers
{
    [Route("api/dashboard-share")]
    public class DashboardShareController : BaseController
    {
        private readonly AppDbContext _context;

        public DashboardShareController(AppDbContext context)
        {
            _context = context;
        }

        // ── POST api/dashboard-share ──────────────────────────────────────────
        /// <summary>Crée un lien de partage pour un dashboard.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateShareRequest request)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(request.DashboardId);

            if (dashboard == null) return NotFound("Dashboard introuvable.");
            if (dashboard.UserId != userId) return Forbid();

            var share = new DashboardShare
            {
                DashboardId      = request.DashboardId,
                SharedWithUserId = request.SharedWithUserId,
                Permission       = request.Permission,
                Token            = Guid.NewGuid().ToString("N"), // token court sans tirets
                CreatedAt        = DateTime.UtcNow,
                ExpiresAt        = request.ExpiresAt
            };

            _context.DashboardShares.Add(share);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                share.Id,
                share.Token,
                share.Permission,
                share.ExpiresAt,
                ShareUrl = $"/api/dashboard-share/access/{share.Token}"
            });
        }

        // ── GET api/dashboard-share/access/{token} ────────────────────────────
        /// <summary>
        /// Accède à un dashboard via son token de partage (sans compte requis).
        /// Retourne un contexte Viewer temporaire — aucun rôle permanent n'est attribué.
        /// </summary>
        [HttpGet("access/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> AccessByToken(string token)
        {
            var share = await _context.DashboardShares
                .Include(s => s.Dashboard)
                    .ThenInclude(d => d!.Widgets)
                .FirstOrDefaultAsync(s => s.Token == token);

            if (share == null)
                return NotFound("Lien de partage invalide.");

            if (share.ExpiresAt.HasValue && share.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Ce lien de partage a expiré.");

            // Contexte Viewer temporaire : communique au frontend les permissions exactes
            // sans créer de compte ni attribuer de rôle permanent en base.
            var viewerContext = new
            {
                role        = "Viewer",           // rôle contextuel, non persisté
                canView     = true,
                canEdit     = share.Permission == SharePermission.Edit,
                isTemporary = true,
                expiresAt   = share.ExpiresAt
            };

            return Ok(new
            {
                share.Permission,
                share.ExpiresAt,
                viewerContext,
                Dashboard = share.Dashboard
            });
        }

        // ── GET api/dashboard-share/{dashboardId} ─────────────────────────────
        /// <summary>Liste tous les partages d'un dashboard (propriétaire uniquement).</summary>
        [HttpGet("{dashboardId:int}")]
        public async Task<IActionResult> GetByDashboard(int dashboardId)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(dashboardId);

            if (dashboard == null) return NotFound();
            if (dashboard.UserId != userId) return Forbid();

            var shares = await _context.DashboardShares
                .Where(s => s.DashboardId == dashboardId)
                .Select(s => new
                {
                    s.Id,
                    s.Token,
                    s.SharedWithUserId,
                    s.Permission,
                    s.CreatedAt,
                    s.ExpiresAt
                })
                .ToListAsync();

            return Ok(shares);
        }

        // ── DELETE api/dashboard-share/{id} ──────────────────────────────────
        /// <summary>Révoque un lien de partage.</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Revoke(int id)
        {
            var userId = GetUserId();
            var share  = await _context.DashboardShares
                .Include(s => s.Dashboard)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (share == null) return NotFound();
            if (share.Dashboard?.UserId != userId) return Forbid();

            _context.DashboardShares.Remove(share);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public record CreateShareRequest(
        int              DashboardId,
        int?             SharedWithUserId,
        SharePermission  Permission,
        DateTime?        ExpiresAt
    );
}