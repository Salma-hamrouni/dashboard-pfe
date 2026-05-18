using System.Globalization;
using System.Text.Json;
using DashboardAPI.Data;
using DashboardAPI.DTOs.Widget;
using Microsoft.EntityFrameworkCore;

namespace DashboardAPI.Services
{
    /// <summary>
    /// Charge les données d'un widget depuis sa DataSource, applique
    /// les filtres et agrégations, puis retourne un payload Chart.js-ready.
    ///
    /// Pipeline : ParseCache → Filter → Aggregate → FormatResponse
    /// </summary>
    public class WidgetDataService
    {
        private readonly AppDbContext _context;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WidgetDataService(AppDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Point d'entrée public
        // ─────────────────────────────────────────────────────────────────────

        public async Task<object> GetWidgetDataAsync(int widgetId, int requestingUserId)
        {
            var widget = await _context.Widgets
                .Include(w => w.Dashboard)
                .FirstOrDefaultAsync(w => w.Id == widgetId)
                ?? throw new KeyNotFoundException($"Widget {widgetId} introuvable.");

            if (widget.Dashboard!.UserId != requestingUserId)
                throw new UnauthorizedAccessException("Accès non autorisé à ce widget.");

            // Pas de config ou pas de DataSourceId → données statiques stockées en DB
            WidgetConfig? config = null;
            if (!string.IsNullOrEmpty(widget.Config))
                config = JsonSerializer.Deserialize<WidgetConfig>(widget.Config, _jsonOpts);

            if (config?.DataSourceId == null)
                return BuildStaticResponse(widget);

            // Charger la DataSource
            var ds = await _context.DataSources
                .FirstOrDefaultAsync(d => d.Id == config.DataSourceId)
                ?? throw new KeyNotFoundException(
                    $"DataSource {config.DataSourceId} introuvable.");

            if (string.IsNullOrEmpty(ds.CachedDataJson))
                throw new InvalidOperationException(
                    "Aucune donnée en cache pour cette source. " +
                    "Effectuez un POST /api/datasource/{id}/refresh d'abord.");

            // Pipeline complet
            var rows = ParseCachedRows(ds.CachedDataJson);
            rows     = ApplyFilter(rows, config);

            var widgetType = widget.Type.ToLowerInvariant();

            return widgetType switch
            {
                WidgetTypes.Table   => BuildTableResponse(widget, rows, config),
                WidgetTypes.Scatter => BuildScatterResponse(widget, rows, config),
                WidgetTypes.Kpi     => BuildKpiResponse(widget,
                                            ApplyAggregation(rows, config), config, rows.Count),
                _ => BuildChartResponse(widget, ApplyAggregation(rows, config), config)
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1. Parsing du cache JSON
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Le cache est : { "columns": [...], "rows": [ {"col": val, ...}, ... ] }
        /// On restitue chaque ligne comme Dictionary[string, object?] avec les types CLR.
        /// </summary>
        private static List<Dictionary<string, object?>> ParseCachedRows(string cachedJson)
        {
            using var doc = JsonDocument.Parse(cachedJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("rows", out var rowsEl) ||
                rowsEl.ValueKind != JsonValueKind.Array)
                return [];

            var result = new List<Dictionary<string, object?>>();

            foreach (var item in rowsEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in item.EnumerateObject())
                    row[prop.Name] = JsonElementToClr(prop.Value);

                result.Add(row);
            }

            return result;
        }

        private static object? JsonElementToClr(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.True   => (object)true,
            JsonValueKind.False  => false,
            JsonValueKind.Null   => null,
            JsonValueKind.Number => el.TryGetDouble(out var d) ? d : (object)el.GetRawText(),
            JsonValueKind.String => el.GetString(),
            _                    => el.GetRawText()
        };

        // ─────────────────────────────────────────────────────────────────────
        // 2. Filtrage
        // ─────────────────────────────────────────────────────────────────────

        private static List<Dictionary<string, object?>> ApplyFilter(
            List<Dictionary<string, object?>> rows,
            WidgetConfig config)
        {
            if (string.IsNullOrEmpty(config.FilterColumn) ||
                string.IsNullOrEmpty(config.FilterValue))
                return rows;

            return rows
                .Where(r => r.TryGetValue(config.FilterColumn, out var v)
                            && v != null
                            && v.ToString()!.Equals(config.FilterValue,
                                StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3. Moteur d'agrégation
        // ─────────────────────────────────────────────────────────────────────

        private record AggRow(string Label, double Value, int Count);

        /// <summary>
        /// Agrège les lignes par GroupBy/XColumn et applique SUM|AVG|COUNT|MIN|MAX.
        /// Sans GroupBy → retourne une seule ligne "Total" (mode KPI).
        /// </summary>
        private static List<AggRow> ApplyAggregation(
            List<Dictionary<string, object?>> rows,
            WidgetConfig config)
        {
            var groupCol = config.GroupBy ?? config.XColumn;
            var yCol     = config.YColumn;
            var agg      = (config.Aggregation ?? "COUNT").ToUpperInvariant();

            if (string.IsNullOrEmpty(groupCol))
            {
                // Agrégation scalaire globale — typiquement pour un KPI
                var total = ComputeAgg(agg, rows, yCol);
                return [new AggRow("Total", total, rows.Count)];
            }

            // GROUP BY
            var groups = rows
                .GroupBy(r => RowString(r, groupCol) ?? "(vide)")
                .Select(g => new AggRow(
                    Label: g.Key,
                    Value: ComputeAgg(agg, g.ToList(), yCol),
                    Count: g.Count()));

            // Tri
            groups = config.SortOrder.Equals("ASC", StringComparison.OrdinalIgnoreCase)
                ? groups.OrderBy(g => g.Value)
                : groups.OrderByDescending(g => g.Value);

            var limit = config.Limit > 0 ? config.Limit : 100;
            return groups.Take(limit).ToList();
        }

        private static double ComputeAgg(
            string agg,
            List<Dictionary<string, object?>> rows,
            string? yCol)
        {
            if (agg == "COUNT" || string.IsNullOrEmpty(yCol))
                return rows.Count;

            var nums = rows
                .Select(r => RowDouble(r, yCol))
                .Where(v => !double.IsNaN(v))
                .ToList();

            if (nums.Count == 0) return 0;

            return agg switch
            {
                "SUM" => nums.Sum(),
                "AVG" => nums.Average(),
                "MIN" => nums.Min(),
                "MAX" => nums.Max(),
                _     => rows.Count
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // 4. Formatage des réponses — format Chart.js
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Format Chart.js standard pour bar / line / pie / donut / area.
        /// { labels, datasets: [{ label, data }], meta }
        /// </summary>
        private static object BuildChartResponse(
            Models.Widget widget,
            List<AggRow> aggregated,
            WidgetConfig config)
        {
            return new
            {
                type   = widget.Type,
                title  = widget.Title,
                labels = aggregated.Select(g => g.Label).ToList(),
                datasets = new[]
                {
                    new
                    {
                        label = BuildDatasetLabel(config),
                        data  = aggregated.Select(g => Math.Round(g.Value, 2)).ToList()
                    }
                },
                meta = new
                {
                    totalGroups  = aggregated.Count,
                    aggregation  = config.Aggregation,
                    xColumn      = config.XColumn,
                    yColumn      = config.YColumn,
                    groupBy      = config.GroupBy,
                    dataSourceId = config.DataSourceId
                }
            };
        }

        /// <summary>
        /// Format KPI : valeur scalaire + formatage humain.
        /// { type, title, value, formatted, aggregation, column, rowsProcessed }
        /// </summary>
        private static object BuildKpiResponse(
            Models.Widget widget,
            List<AggRow> aggregated,
            WidgetConfig config,
            int totalRows)
        {
            var value = aggregated.FirstOrDefault()?.Value ?? 0;

            return new
            {
                type          = WidgetTypes.Kpi,
                title         = widget.Title,
                value         = Math.Round(value, 2),
                formatted     = FormatHuman(value),
                aggregation   = config.Aggregation,
                column        = config.YColumn,
                rowsProcessed = totalRows,
                dataSourceId  = config.DataSourceId
            };
        }

        /// <summary>
        /// Format Table : colonnes + lignes brutes.
        /// { type, columns, rows, totalRows }
        /// </summary>
        private static object BuildTableResponse(
            Models.Widget widget,
            List<Dictionary<string, object?>> rows,
            WidgetConfig config)
        {
            var limit   = config.Limit > 0 ? config.Limit : 100;
            var limited = rows.Take(limit).ToList();
            var cols    = limited.FirstOrDefault()?.Keys.ToList() ?? [];

            return new
            {
                type      = WidgetTypes.Table,
                title     = widget.Title,
                columns   = cols,
                rows      = limited
                                .Select(r => cols
                                    .Select(c => r.TryGetValue(c, out var v) ? v : null)
                                    .ToList())
                                .ToList(),
                totalRows = rows.Count
            };
        }

        /// <summary>
        /// Format Scatter : points { x, y }.
        /// { type, datasets: [{ label, data: [{x,y}] }] }
        /// </summary>
        private static object BuildScatterResponse(
            Models.Widget widget,
            List<Dictionary<string, object?>> rows,
            WidgetConfig config)
        {
            var limit  = config.Limit > 0 ? config.Limit : 200;
            var points = rows
                .Take(limit)
                .Select(r => new
                {
                    x = Math.Round(RowDouble(r, config.XColumn ?? ""), 4),
                    y = Math.Round(RowDouble(r, config.YColumn ?? ""), 4)
                })
                .Where(p => !double.IsNaN(p.x) && !double.IsNaN(p.y))
                .ToList();

            return new
            {
                type     = WidgetTypes.Scatter,
                title    = widget.Title,
                datasets = new[]
                {
                    new { label = widget.Title, data = points }
                },
                meta = new
                {
                    xColumn = config.XColumn,
                    yColumn = config.YColumn,
                    count   = points.Count
                }
            };
        }

        /// <summary>
        /// Fallback : données statiques JSON stockées dans Widget.Data.
        /// </summary>
        private static object BuildStaticResponse(Models.Widget widget)
        {
            if (string.IsNullOrEmpty(widget.Data))
                return new { type = widget.Type, title = widget.Title, data = (object?)null };

            try
            {
                using var doc = JsonDocument.Parse(widget.Data);
                return new
                {
                    type  = widget.Type,
                    title = widget.Title,
                    data  = JsonSerializer.Deserialize<object>(widget.Data)
                };
            }
            catch
            {
                return new { type = widget.Type, title = widget.Title, data = widget.Data };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────

        private static double RowDouble(Dictionary<string, object?> row, string col)
        {
            if (string.IsNullOrEmpty(col) ||
                !row.TryGetValue(col, out var v) || v is null)
                return double.NaN;

            return v switch
            {
                double d  => d,
                float f   => f,
                int i     => i,
                long l    => l,
                decimal m => (double)m,
                string s  => double.TryParse(s, NumberStyles.Any,
                                 CultureInfo.InvariantCulture, out var p) ? p : double.NaN,
                _         => double.TryParse(v.ToString(), NumberStyles.Any,
                                 CultureInfo.InvariantCulture, out var p2) ? p2 : double.NaN
            };
        }

        private static string? RowString(Dictionary<string, object?> row, string col)
        {
            if (!row.TryGetValue(col, out var v)) return null;
            return v switch { null => null, string s => s, _ => v.ToString() };
        }

        private static string BuildDatasetLabel(WidgetConfig config)
        {
            if (!string.IsNullOrEmpty(config.YColumn))
                return $"{config.Aggregation} of {config.YColumn}";
            return config.Aggregation == "COUNT" ? "Count" : config.Aggregation;
        }

        /// <summary>Formatage lisible : 1 500 000 → "1.5M", 2 300 → "2.3K".</summary>
        private static string FormatHuman(double v)
        {
            if (v >= 1_000_000)
            {
                var d = v / 1_000_000;
                return d % 1 == 0
                    ? $"{(long)d}M"
                    : $"{d.ToString("F1", CultureInfo.InvariantCulture)}M";
            }
            if (v >= 1_000)
            {
                var d = v / 1_000;
                return d % 1 == 0
                    ? $"{(long)d}K"
                    : $"{d.ToString("F1", CultureInfo.InvariantCulture)}K";
            }
            return v % 1 == 0
                ? ((long)v).ToString(CultureInfo.InvariantCulture)
                : v.ToString("N2", CultureInfo.InvariantCulture);
        }
    }
}
