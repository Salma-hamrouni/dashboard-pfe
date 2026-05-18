using Microsoft.AspNetCore.Mvc;
using DashboardAPI.Data;
using DashboardAPI.Models;
using DashboardAPI.Services;
using DashboardAPI.DTOs.Widget;
using DashboardAPI.Common;
using System.Text.Json;

namespace DashboardAPI.Controllers
{
    [Route("api/[controller]")]
    public class WidgetController(
        WidgetDao widgetDao,
        AppDbContext context,
        WidgetDataService widgetDataService,
        AuditService audit) : BaseController
    {
        private static readonly JsonSerializerOptions _camelCase = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // ── GET api/widget/{dashboardId} ──────────────────────────────────────
        [HttpGet("{dashboardId:int}")]
        [ProducesResponseType(typeof(ApiResponse<List<WidgetResponseDto>>), 200)]
        public async Task<IActionResult> GetByDashboard(int dashboardId)
        {
            var userId    = GetUserId();
            var dashboard = await context.Dashboards.FindAsync(dashboardId);

            if (dashboard == null)
                return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));

            if (dashboard.UserId != userId && !dashboard.IsPublic)
                return Forbid();

            var widgets = await widgetDao.GetByDashboardAsync(dashboardId);

            var dtos = widgets.Select(w => new WidgetResponseDto
            {
                Id          = w.Id,
                Type        = w.Type,
                Title       = w.Title,
                Data        = w.Data,
                Config      = w.Config,
                DashboardId = w.DashboardId
            }).ToList();

            return Ok(ApiResponse<List<WidgetResponseDto>>.Ok(dtos,
                new { total = dtos.Count }));
        }

        // ── POST api/widget ───────────────────────────────────────────────────
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<WidgetResponseDto>), 201)]
        public async Task<IActionResult> Add([FromBody] Widget widget)
        {
            var userId    = GetUserId();
            var dashboard = await context.Dashboards.FindAsync(widget.DashboardId);

            if (dashboard == null)
                return NotFound(ApiResponse<object>.Fail("Dashboard introuvable."));

            if (dashboard.UserId != userId)
                return Forbid();

            var result = await widgetDao.AddAsync(widget);

            await audit.LogAsync(userId, "WIDGET_CREATED", "Widget", result.Id,
                $"type={result.Type} dashboardId={result.DashboardId}", GetClientIp());

            return CreatedAtAction(nameof(GetByDashboard),
                new { dashboardId = result.DashboardId },
                ApiResponse<WidgetResponseDto>.Ok(new WidgetResponseDto
                {
                    Id          = result.Id,
                    Type        = result.Type,
                    Title       = result.Title,
                    Data        = result.Data,
                    Config      = result.Config,
                    DashboardId = result.DashboardId
                }));
        }

        // ── PUT api/widget/{id} ───────────────────────────────────────────────
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<WidgetResponseDto>), 200)]
        public async Task<IActionResult> Update(int id, [FromBody] Widget updated)
        {
            var userId = GetUserId();
            var widget = await widgetDao.GetByIdAsync(id);

            if (widget == null) return NotFound(ApiResponse<object>.Fail("Widget introuvable."));

            var dashboard = await context.Dashboards.FindAsync(widget.DashboardId);
            if (dashboard == null || dashboard.UserId != userId) return Forbid();

            widget.Type  = updated.Type;
            widget.Title = updated.Title;
            widget.Data  = updated.Data;

            var result = await widgetDao.UpdateAsync(widget);

            await audit.LogAsync(userId, "WIDGET_UPDATED", "Widget", id,
                $"type={result.Type}", GetClientIp());

            return Ok(ApiResponse<WidgetResponseDto>.Ok(new WidgetResponseDto
            {
                Id          = result.Id,
                Type        = result.Type,
                Title       = result.Title,
                Data        = result.Data,
                Config      = result.Config,
                DashboardId = result.DashboardId
            }));
        }

        // ── DELETE api/widget/{id} ────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var widget = await widgetDao.GetByIdAsync(id);

            if (widget == null) return NotFound(ApiResponse<object>.Fail("Widget introuvable."));

            var dashboard = await context.Dashboards.FindAsync(widget.DashboardId);
            if (dashboard == null || dashboard.UserId != userId) return Forbid();

            await widgetDao.DeleteAsync(id);

            await audit.LogAsync(userId, "WIDGET_DELETED", "Widget", id,
                $"type={widget.Type} dashboardId={widget.DashboardId}", GetClientIp());

            return NoContent();
        }

        // ── GET api/widget/{id}/data ──────────────────────────────────────────
        /// <summary>
        /// Charge les données dynamiques d'un widget depuis sa DataSource configurée.
        /// Pipeline : parse cache → filtre → agrégation → format Chart.js.
        /// </summary>
        [HttpGet("{id:int}/data")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetData(int id)
        {
            var data = await widgetDataService.GetWidgetDataAsync(id, GetUserId());
            return Ok(ApiResponse<object>.Ok(data));
        }

        // ── PUT api/widget/{id}/config ────────────────────────────────────────
        /// <summary>
        /// Lie un widget à une DataSource et configure l'agrégation
        /// (xColumn, yColumn, aggregation, groupBy, filterColumn/Value, limit).
        /// </summary>
        [HttpPut("{id:int}/config")]
        [ProducesResponseType(typeof(ApiResponse<WidgetResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UpdateConfig(int id, [FromBody] WidgetConfig config)
        {
            var userId = GetUserId();
            var widget = await widgetDao.GetByIdAsync(id);
            if (widget == null) return NotFound(ApiResponse<object>.Fail("Widget introuvable."));

            var dashboard = await context.Dashboards.FindAsync(widget.DashboardId);
            if (dashboard == null || dashboard.UserId != userId)
                return Forbid();

            if (config.DataSourceId.HasValue)
            {
                var ds = await context.DataSources.FindAsync(config.DataSourceId.Value);
                if (ds == null || ds.UserId != userId)
                    return BadRequest(ApiResponse<object>.Fail(
                        $"DataSource {config.DataSourceId} introuvable ou non autorisée."));
            }

            widget.Config = JsonSerializer.Serialize(config, _camelCase);
            await widgetDao.UpdateAsync(widget);

            await audit.LogAsync(userId, "WIDGET_CONFIG_UPDATED", "Widget", id,
                $"dataSourceId={config.DataSourceId} agg={config.Aggregation}", GetClientIp());

            return Ok(ApiResponse<WidgetResponseDto>.Ok(new WidgetResponseDto
            {
                Id          = widget.Id,
                Type        = widget.Type,
                Title       = widget.Title,
                Data        = widget.Data,
                Config      = widget.Config,
                DashboardId = widget.DashboardId
            }));
        }
    }
}
