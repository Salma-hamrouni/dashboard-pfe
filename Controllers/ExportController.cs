using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using DashboardAPI.Data;
using DashboardAPI.Models;

namespace DashboardAPI.Controllers
{
    [Route("api/export")]
    public class ExportController : BaseController
    {
        private readonly AppDbContext _context;

        public ExportController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET api/export/{id}/json ───────────────────────────────────────────
        /// <summary>
        /// Exporte la configuration complète d'un dashboard en JSON
        /// (widgets, layout, datasource) — téléchargeable.
        /// </summary>
        [HttpGet("{id:int}/json")]
        public async Task<IActionResult> ExportJson(int id)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dashboard == null) return NotFound("Dashboard introuvable.");
            if (dashboard.UserId != userId && !dashboard.IsPublic) return Forbid();

            var export = new
            {
                exportedAt = DateTime.UtcNow,
                version    = "1.0",
                dashboard  = new
                {
                    dashboard.Id,
                    dashboard.Name,
                    dashboard.DatasetId,
                    dashboard.IsPublic,
                    dashboard.CreatedAt,
                    columns         = TryDeserialize(dashboard.ColumnsJson),
                    insights        = TryDeserialize(dashboard.InsightsJson),
                    recommendations = TryDeserialize(dashboard.RecommendationsJson),
                    widgets = dashboard.Widgets.Select(w => new
                    {
                        w.Id,
                        w.Type,
                        w.Title,
                        data = TryDeserializeObject(w.Data)
                    })
                }
            };

            var json     = JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true });
            var bytes    = Encoding.UTF8.GetBytes(json);
            var fileName = $"dashboard_{dashboard.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";

            return File(bytes, "application/json", fileName);
        }

        // ── POST api/export/import-json ───────────────────────────────────────
        /// <summary>
        /// Importe une configuration JSON exportée et recrée le dashboard
        /// pour l'utilisateur connecté.
        /// </summary>
        [HttpPost("import-json")]
        public async Task<IActionResult> ImportJson(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Fichier JSON vide.");

            if (Path.GetExtension(file.FileName).ToLower() != ".json")
                return BadRequest("Seuls les fichiers .json sont acceptés.");

            string json;
            using (var reader = new StreamReader(file.OpenReadStream()))
                json = await reader.ReadToEndAsync();

            JsonElement root;
            try
            {
                root = JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                return BadRequest("JSON invalide.");
            }

            if (!root.TryGetProperty("dashboard", out var dashboardEl))
                return BadRequest("Format JSON non reconnu. Clé 'dashboard' manquante.");

            // Recréer le dashboard
            var dashboard = new Dashboard
            {
                Name                = dashboardEl.TryGetProperty("name", out var n) ? n.GetString() ?? "Dashboard importé" : "Dashboard importé",
                DatasetId           = dashboardEl.TryGetProperty("datasetId", out var did) ? did.GetInt32() : 0,
                UserId              = GetUserId(),
                IsPublic            = false,
                ShareToken          = Guid.NewGuid().ToString(),
                CreatedAt           = DateTime.UtcNow,
                ColumnsJson         = dashboardEl.TryGetProperty("columns", out var cols) ? cols.GetRawText() : "[]",
                InsightsJson        = dashboardEl.TryGetProperty("insights", out var ins) ? ins.GetRawText() : "[]",
                RecommendationsJson = dashboardEl.TryGetProperty("recommendations", out var rec) ? rec.GetRawText() : "[]",
            };

            _context.Dashboards.Add(dashboard);
            await _context.SaveChangesAsync();

            // Recréer les widgets
            if (dashboardEl.TryGetProperty("widgets", out var widgetsEl))
            {
                foreach (var w in widgetsEl.EnumerateArray())
                {
                    _context.Widgets.Add(new Widget
                    {
                        DashboardId = dashboard.Id,
                        Type        = w.TryGetProperty("type",  out var t) ? t.GetString() ?? "bar" : "bar",
                        Title       = w.TryGetProperty("title", out var ti) ? ti.GetString() ?? "" : "",
                        Data        = w.TryGetProperty("data",  out var d) ? d.GetRawText() : "{}"
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message      = "Dashboard importé avec succès.",
                dashboardId  = dashboard.Id,
                dashboard.Name
            });
        }

        // ── GET api/export/{id}/pdf ───────────────────────────────────────────
        /// <summary>
        /// Génère un PDF du dashboard côté serveur.
        /// Retourne un PDF avec les infos du dashboard et la liste des widgets.
        /// </summary>
        [HttpGet("{id:int}/pdf")]
        public async Task<IActionResult> ExportPdf(int id)
        {
            var userId    = GetUserId();
            var dashboard = await _context.Dashboards
                .Include(d => d.Widgets)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dashboard == null) return NotFound("Dashboard introuvable.");
            if (dashboard.UserId != userId && !dashboard.IsPublic) return Forbid();

            var pdf      = GeneratePdf(dashboard);
            var fileName = $"dashboard_{dashboard.Id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdf, "application/pdf", fileName);
        }

        // ── Helper : génération PDF minimal sans dépendance externe ──────────
        private static byte[] GeneratePdf(Dashboard dashboard)
        {
            // PDF généré manuellement (format PDF 1.4 minimal)
            // Pour un PDF riche, intégrer QuestPDF ou DinkToPdf plus tard
            var sb = new StringBuilder();
            var now = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm");

            // Contenu texte du PDF
            var lines = new List<string>
            {
                $"DASHBOARD : {dashboard.Name}",
                $"Exporté le : {now}",
                $"ID : {dashboard.Id}",
                $"Public : {(dashboard.IsPublic ? "Oui" : "Non")}",
                $"Créé le : {dashboard.CreatedAt:dd/MM/yyyy}",
                "",
                $"WIDGETS ({dashboard.Widgets?.Count ?? 0}) :",
                ""
            };

            if (dashboard.Widgets != null)
            {
                foreach (var w in dashboard.Widgets)
                {
                    lines.Add($"  [{w.Type.ToUpper()}] {w.Title}");
                    lines.Add($"  ID: {w.Id}");
                    lines.Add("");
                }
            }

            // Construction PDF brut
            var objects   = new List<string>();
            var offsets   = new List<int>();
            var body      = new StringBuilder();

            // Objet 1 : catalog
            objects.Add("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
            // Objet 2 : pages
            objects.Add("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            // Objet 4 : font
            objects.Add("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

            // Contenu de la page
            var content = new StringBuilder();
            content.AppendLine("BT");
            content.AppendLine("/F1 14 Tf");
            content.AppendLine("50 780 Td");
            content.AppendLine("14 TL");

            foreach (var line in lines)
            {
                var safe = line
                    .Replace("\\", "\\\\")
                    .Replace("(", "\\(")
                    .Replace(")", "\\)")
                    .Replace("\r", "")
                    .Replace("\n", "");
                content.AppendLine($"({safe}) Tj T*");
            }
            content.AppendLine("ET");

            var contentStr  = content.ToString();
            var contentBytes = Encoding.Latin1.GetBytes(contentStr);

            // Objet 5 : stream contenu
            objects.Add($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n{contentStr}\nendstream\nendobj\n");

            // Objet 3 : page
            objects.Add("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 5 0 R /Resources << /Font << /F1 4 0 R >> >> >>\nendobj\n");

            // Assembler le PDF
            var pdfSb  = new StringBuilder();
            pdfSb.AppendLine("%PDF-1.4");

            var currentOffset = Encoding.Latin1.GetByteCount("%PDF-1.4\n");
            foreach (var obj in objects)
            {
                offsets.Add(currentOffset);
                pdfSb.Append(obj);
                currentOffset += Encoding.Latin1.GetByteCount(obj);
            }

            var xrefOffset = currentOffset;
            pdfSb.AppendLine("xref");
            pdfSb.AppendLine($"0 {objects.Count + 1}");
            pdfSb.AppendLine("0000000000 65535 f ");
            foreach (var off in offsets)
                pdfSb.AppendLine($"{off:D10} 00000 n ");

            pdfSb.AppendLine("trailer");
            pdfSb.AppendLine($"<< /Size {objects.Count + 1} /Root 1 0 R >>");
            pdfSb.AppendLine("startxref");
            pdfSb.AppendLine(xrefOffset.ToString());
            pdfSb.AppendLine("%%EOF");

            return Encoding.Latin1.GetBytes(pdfSb.ToString());
        }

        // ── Helpers désérialisation ───────────────────────────────────────────
        private static object? TryDeserialize(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json); }
            catch { return json; }
        }

        private static object? TryDeserializeObject(string? json)
        {
            if (string.IsNullOrEmpty(json)) return new { };
            try { return JsonSerializer.Deserialize<JsonElement>(json); }
            catch { return json; }
        }
    }
}