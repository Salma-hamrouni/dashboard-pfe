using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using DashboardAPI.Data;
using DashboardAPI.DTOs;
using DashboardAPI.Models;

namespace DashboardAPI.Controllers
{
    [Route("api/ai")]
    public class AiController : BaseController
    {
        private readonly AppDbContext   _context;
        private readonly IConfiguration _config;
        private readonly HttpClient     _http;

        public AiController(AppDbContext context, IConfiguration config, IHttpClientFactory httpFactory)
        {
            _context = context;
            _config  = config;
            _http    = httpFactory.CreateClient();
        }

        // ── POST api/ai/recommend-chart ───────────────────────────────────────
        /// <summary>
        /// Analyse les colonnes d'une source de données et recommande
        /// le type de graphique le plus adapté avec justification.
        /// </summary>
        [HttpPost("recommend-chart")]
        public async Task<IActionResult> RecommendChart([FromBody] AiColumnRequest request)
        {
            if (request.Columns == null || !request.Columns.Any())
                return BadRequest("Aucune colonne fournie.");

            var columnsDesc = string.Join("\n", request.Columns.Select(c =>
                $"- {c.Name} (type: {c.Type})"));

            var prompt = $@"
Tu es un expert en visualisation de données.
Voici les colonnes d'un dataset :
{columnsDesc}

Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après.
Schéma attendu :
{{
  ""recommendedChart"": ""string"",
  ""reason"": ""string"",
  ""alternatives"": [""string""],
  ""xAxis"": ""string"",
  ""yAxis"": ""string""
}}
";
            var result = await CallGeminiAsync(prompt);
            return Ok(result);
        }

        // ── POST api/ai/suggest-kpis ──────────────────────────────────────────
        /// <summary>
        /// À partir des colonnes du dataset, propose des KPIs pertinents
        /// avec leur agrégation et colonne source.
        /// </summary>
        [HttpPost("suggest-kpis")]
        public async Task<IActionResult> SuggestKpis([FromBody] AiColumnRequest request)
        {
            if (request.Columns == null || !request.Columns.Any())
                return BadRequest("Aucune colonne fournie.");

            var columnsDesc = string.Join("\n", request.Columns.Select(c =>
                $"- {c.Name} (type: {c.Type})"));

            var prompt = $@"
Tu es un expert en business intelligence.
Voici les colonnes d'un dataset :
{columnsDesc}

Propose des KPIs pertinents à afficher sur un tableau de bord.
Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après.
Schéma attendu :
{{
  ""kpis"": [
    {{
      ""title"": ""string"",
      ""column"": ""string"",
      ""aggregation"": ""sum | avg | count | min | max"",
      ""format"": ""number | percent | currency"",
      ""description"": ""string""
    }}
  ]
}}
";
            var result = await CallGeminiAsync(prompt);
            return Ok(result);
        }

        // ── POST api/ai/analyze ───────────────────────────────────────────────
        /// <summary>
        /// Génère un résumé textuel des données :
        /// tendances, anomalies, et insights automatiques.
        /// </summary>
        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] AiAnalyzeRequest request)
        {
            if (request.Columns == null || !request.Columns.Any())
                return BadRequest("Aucune colonne fournie.");

            var columnsDesc = string.Join("\n", request.Columns.Select(c =>
                $"- {c.Name} (type: {c.Type})"));

            var previewDesc = request.Preview != null && request.Preview.Any()
                ? JsonSerializer.Serialize(request.Preview.Take(10))
                : "Non fourni";

            var prompt = $@"
Tu es un data analyst expert.
Voici un dataset avec ses colonnes et un aperçu des données :

Colonnes :
{columnsDesc}

Aperçu (premières lignes) :
{previewDesc}

Analyse ces données et détecte les tendances, anomalies et insights importants.
Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après.
Schéma attendu :
{{
  ""summary"": ""string"",
  ""insights"": [""string""],
  ""trends"": [""string""],
  ""anomalies"": [""string""],
  ""recommendations"": [""string""]
}}
";
            var result = await CallGeminiAsync(prompt);
            return Ok(result);
        }

        // ── POST api/ai/chat ──────────────────────────────────────────────────
        /// <summary>
        /// Assistant conversationnel : l'utilisateur décrit un graphique
        /// en langage naturel, l'IA retourne une config widget JSON.
        /// </summary>
        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest("Message vide.");

            var prompt = $@"
Tu es un assistant pour générateur de tableaux de bord.
L'utilisateur décrit ce qu'il veut visualiser :
""{request.Message}""

{(request.Columns != null && request.Columns.Any()
    ? $"Colonnes disponibles :\n{string.Join("\n", request.Columns.Select(c => $"- {c.Name} ({c.Type})"))}"
    : "")}

Génère une configuration de widget JSON.
Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après.
Schéma attendu :
{{
  ""type"": ""bar | line | pie | scatter | kpi | table"",
  ""title"": ""string"",
  ""xAxis"": ""string or null"",
  ""yAxis"": ""string or null"",
  ""aggregation"": ""sum | avg | count | none"",
  ""filters"": [""string""],
  ""explanation"": ""string""
}}
";
            var result = await CallGeminiAsync(prompt);
            return Ok(result);
        }

        // ── Helper : appel Gemini ─────────────────────────────────────────────
        private async Task<object> CallGeminiAsync(string prompt)
        {
            var apiKey = _config["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey manquant dans appsettings.");

            var body = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}"
            );
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(httpRequest);
            var json     = await response.Content.ReadAsStringAsync();

            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates))
                throw new Exception("Erreur Gemini : " + json);

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? throw new Exception("Réponse IA vide.");

            // Nettoyer les balises markdown si présentes
            text = text.Replace("```json", "").Replace("```", "").Trim();

            // Parser et retourner comme objet dynamique
            return JsonSerializer.Deserialize<JsonElement>(text);
        }
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class AiColumnRequest
    {
        public List<ColumnDto> Columns { get; set; } = new();
    }

    public class AiAnalyzeRequest
    {
        public List<ColumnDto>                       Columns { get; set; } = new();
        public List<Dictionary<string, object?>>?    Preview { get; set; }
    }

    public class AiChatRequest
    {
        public string          Message { get; set; } = string.Empty;
        public List<ColumnDto>? Columns { get; set; }
    }

    public class ColumnDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}