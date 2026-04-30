using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using DashboardAPI.Data;
using DashboardAPI.Models;

namespace DashboardAPI.Controllers
{
    [Route("api/[controller]")]
    public class WidgetController : BaseController
    {
        private readonly WidgetDao    _widgetDao;
        private readonly AppDbContext _context;

        public WidgetController(WidgetDao widgetDao, AppDbContext context)
        {
            _widgetDao = widgetDao;
            _context   = context;
        }

        // ── GET api/widget/{dashboardId} ──────────────────────────────────────
        /// <summary>Retourne les widgets d'un dashboard, si le dashboard appartient au user.</summary>
        [HttpGet("{dashboardId:int}")]
        public async Task<IActionResult> GetByDashboard(int dashboardId)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(dashboardId);

            if (dashboard == null) return NotFound("Dashboard introuvable.");

            // ✅ Seul le proprio (ou dashboard public) peut voir les widgets
            if (dashboard.UserId != userId && !dashboard.IsPublic)
                return Forbid();

            var widgets = await _widgetDao.GetByDashboardAsync(dashboardId);
            return Ok(widgets);
        }

        // ── POST api/widget ───────────────────────────────────────────────────
        /// <summary>Ajoute un widget à un dashboard appartenant au user connecté.</summary>
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Widget widget)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards.FindAsync(widget.DashboardId);

            if (dashboard == null) return NotFound("Dashboard introuvable.");
            if (dashboard.UserId != userId) return Forbid(); // ✅ on ne peut pas ajouter sur le dashboard d'un autre

            var result = await _widgetDao.AddAsync(widget);
            return CreatedAtAction(nameof(GetByDashboard),
                new { dashboardId = result.DashboardId }, result);
        }

        // ── PUT api/widget/{id} ───────────────────────────────────────────────
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Widget updated)
        {
            var userId = GetUserId();
            var widget = await _widgetDao.GetByIdAsync(id);

            if (widget == null) return NotFound();

            // Vérifier que le dashboard du widget appartient au user
            var dashboard = await _context.Dashboards.FindAsync(widget.DashboardId);
            if (dashboard == null || dashboard.UserId != userId) return Forbid();

            widget.Type  = updated.Type;
            widget.Title = updated.Title;
            widget.Data  = updated.Data;

            var result = await _widgetDao.UpdateAsync(widget);
            return Ok(result);
        }

        // ── DELETE api/widget/{id} ────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var widget = await _widgetDao.GetByIdAsync(id);

            if (widget == null) return NotFound();

            // ✅ Vérifier que le dashboard appartient au user avant de supprimer
            var dashboard = await _context.Dashboards.FindAsync(widget.DashboardId);
            if (dashboard == null || dashboard.UserId != userId) return Forbid();

            await _widgetDao.DeleteAsync(id);
            return NoContent();
        }
    }
}