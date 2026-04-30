using Microsoft.AspNetCore.Mvc;
using CsvHelper;
using System.Globalization;
using System.Text.Json;
using DashboardAPI.Models;
using DashboardAPI.Data;
using DashboardAPI.DTOs;
using DashboardAPI.Services;

namespace DashboardAPI.Controllers
{
    [Route("api/datasets")]
    public class DatasetController : BaseController
    {
        private readonly AppDbContext _context;
        private readonly AiService    _ai;

        public DatasetController(AppDbContext context, AiService ai)
        {
            _context = context;
            _ai      = ai;
        }

        // ── POST api/datasets/upload ──────────────────────────────────────────
        /// <summary>Upload un CSV et retourne un aperçu (5 lignes) + colonnes.</summary>
        [HttpPost("upload")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Fichier vide.");

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv    = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = new List<Dictionary<string, string>>();

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                var row = new Dictionary<string, string>();
                foreach (var header in csv.HeaderRecord!)
                    row[header] = csv.GetField(header) ?? string.Empty;
                records.Add(row);
            }

            return Ok(new
            {
                columns   = csv.HeaderRecord,
                preview   = records.Take(5),
                totalRows = records.Count
            });
        }

        // ── POST api/datasets/analyze ─────────────────────────────────────────
        /// <summary>Détecte les types de chaque colonne.</summary>
        [HttpPost("analyze")]
        public IActionResult Analyze([FromBody] List<Dictionary<string, string>> rows)
        {
            if (rows == null || !rows.Any())
                return BadRequest("Aucune donnée.");

            var firstRow = rows.First();
            var profile  = firstRow.Keys.Select(col => new ColumnProfile
            {
                Name = col,
                Type = DetectType(firstRow[col])
            }).ToList();

            return Ok(profile);
        }

        // ── POST api/datasets/recommend ───────────────────────────────────────
        /// <summary>Recommande des types de graphiques selon les colonnes.</summary>
        [HttpPost("recommend")]
        public IActionResult Recommend([FromBody] List<ColumnProfile> columns)
        {
            if (columns == null || !columns.Any())
                return BadRequest("Aucune colonne.");

            var charts = new List<object>();

            for (int i = 0; i < columns.Count; i++)
            {
                for (int j = 0; j < columns.Count; j++)
                {
                    if (i == j) continue;
                    var x = columns[i];
                    var y = columns[j];
                    charts.Add(new
                    {
                        x     = x.Name,
                        y     = y.Name,
                        chart = RecommendChart(x.Name, x.Type, y.Name, y.Type)
                    });
                }
            }

            return Ok(charts.Take(10));
        }

        // ── POST api/datasets/ai-analyze ──────────────────────────────────────
        /// <summary>
        /// Analyse les données avec l'IA, crée un dashboard + widgets automatiquement
        /// et les associe à l'utilisateur connecté.
        /// </summary>
        [HttpPost("ai-analyze")]
        public async Task<IActionResult> AnalyzeWithAI([FromBody] List<DatasetRequest> rows)
        {
            if (rows == null || !rows.Any())
                return BadRequest("Aucune donnée.");

            var userId = GetUserId(); // ✅ depuis le token JWT
            var result = await _ai.AnalyzeAsync(rows[0]);

            // Créer le dashboard lié à l'utilisateur connecté
            var dashboard = new Dashboard
            {
                Name                = "Dashboard " + DateTime.UtcNow.ToString("HH:mm"),
                DatasetId           = 0,
                UserId              = userId,             // ✅ plus de userId=1 en dur
                InsightsJson        = JsonSerializer.Serialize(result.Insights),
                RecommendationsJson = JsonSerializer.Serialize(result.Recommendations),
                ShareToken          = Guid.NewGuid().ToString(),
                IsPublic            = false,
                CreatedAt           = DateTime.UtcNow
            };

            _context.Dashboards.Add(dashboard);
            await _context.SaveChangesAsync(); // ✅ sauvegarde réelle en base

            // Créer les widgets automatiques
            var widgets = new List<Widget>
            {
                new() {
                    DashboardId = dashboard.Id,
                    Type  = "bar",
                    Title = "Analyse des données (Bar Chart)",
                    Data  = JsonSerializer.Serialize(new {
                        labels = new[] { "A", "B", "C", "D" },
                        values = new[] { 120, 200, 150, 300 }
                    })
                },
                new() {
                    DashboardId = dashboard.Id,
                    Type  = "line",
                    Title = "Tendance temporelle",
                    Data  = JsonSerializer.Serialize(new {
                        labels = new[] { "Jan", "Fév", "Mar", "Avr" },
                        values = new[] { 100, 180, 250, 400 }
                    })
                },
                new() {
                    DashboardId = dashboard.Id,
                    Type  = "kpi",
                    Title = "Score global",
                    Data  = JsonSerializer.Serialize(new { value = 87 })
                },
                new() {
                    DashboardId = dashboard.Id,
                    Type  = "insight",
                    Title = "AI Insight",
                    Data  = JsonSerializer.Serialize(new {
                        text = string.Join(" | ", result.Insights)
                    })
                }
            };

            _context.Widgets.AddRange(widgets); // ✅ sauvegarde réelle en base
            await _context.SaveChangesAsync();

            return Ok(new
            {
                dashboard.Id,
                dashboard.Name,
                dashboard.ShareToken,
                Insights        = result.Insights,
                Recommendations = result.Recommendations,
                WidgetsCreated  = widgets.Count
            });
        }

        // ── Helpers privés ────────────────────────────────────────────────────
        private static string RecommendChart(string colX, string typeX, string colY, string typeY)
            => (typeX, typeY) switch
            {
                ("date",     "number") => "line chart",
                ("category", "number") => "bar chart",
                ("number",   "number") => "scatter plot",
                _                      => "table"
            };

        private static string DetectType(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            if (int.TryParse(value, out _))        return "number";
            if (double.TryParse(value, out _))     return "number";
            if (DateTime.TryParse(value, out _))   return "date";
            return "category";
        }
    }
}