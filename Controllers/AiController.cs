using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DashboardAPI.Common;
using DashboardAPI.Data;
using DashboardAPI.DTOs;

namespace DashboardAPI.Controllers
{
    [Route("api/ai")]
    [EnableRateLimiting("ai-policy")]
    public class AiController(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        IMemoryCache cache,
        DataSourceDao dataSourceDao,
        ILogger<AiController> logger) : BaseController
    {
        private readonly HttpClient _http = httpFactory.CreateClient("gemini");
        private readonly DataSourceDao _dataSourceDao = dataSourceDao;

        private static readonly MemoryCacheEntryOptions _cacheOpts = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
            SlidingExpiration = TimeSpan.FromMinutes(20),
            Size = 1
        };

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves columns (and optionally preview rows) from either the request body
        /// or a datasource looked up by id. Returns null if the datasource is not found
        /// or does not belong to the current user.
        /// </summary>
        private async Task<(List<ColumnDto>? columns, List<Dictionary<string, object?>>? preview, IActionResult? error)>
            ResolveColumnsAsync(int? dataSourceId, List<ColumnDto>? inlineColumns, bool includePreview = false)
        {
            if (inlineColumns?.Count > 0)
                return (inlineColumns, null, null);

            if (dataSourceId == null)
                return (null, null, BadRequest(ApiResponse<object>.Fail("Fournissez 'dataSourceId' ou 'columns'.")));

            var ds = await _dataSourceDao.GetByIdAsync(dataSourceId.Value);
            if (ds == null || ds.UserId != GetUserId())
                return (null, null, NotFound(ApiResponse<object>.Fail("Source de données introuvable.")));

            if (string.IsNullOrEmpty(ds.CachedDataJson))
                return (null, null, BadRequest(ApiResponse<object>.Fail("Source de données vide. Effectuez d'abord un refresh.")));

            using var doc = JsonDocument.Parse(ds.CachedDataJson);
            var root = doc.RootElement;

            var columns = new List<ColumnDto>();
            if (root.TryGetProperty("columns", out var colsEl))
                foreach (var c in colsEl.EnumerateArray())
                    columns.Add(new ColumnDto
                    {
                        Name = c.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Type = c.TryGetProperty("type", out var t) ? t.GetString() ?? "string" : "string"
                    });

            List<Dictionary<string, object?>>? preview = null;
            if (includePreview && root.TryGetProperty("rows", out var rowsEl))
            {
                preview = [];
                foreach (var row in rowsEl.EnumerateArray().Take(10))
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in row.EnumerateObject())
                        dict[prop.Name] = prop.Value.ValueKind switch
                        {
                            JsonValueKind.Number => prop.Value.TryGetDouble(out var d) ? (object?)d : null,
                            JsonValueKind.True   => true,
                            JsonValueKind.False  => false,
                            JsonValueKind.Null   => null,
                            _                    => prop.Value.GetString()
                        };
                    preview.Add(dict);
                }
            }

            return (columns, preview, null);
        }

        // ── POST api/ai/recommend-chart ───────────────────────────────────────
        /// <summary>Recommande le type de graphique le plus adapté aux colonnes fournies.</summary>
        [HttpPost("recommend-chart")]
        [ProducesResponseType(typeof(ApiResponse<JsonElement>), 200)]
        public async Task<IActionResult> RecommendChart([FromBody] AiColumnRequest request)
        {
            var (columns, _, error) = await ResolveColumnsAsync(request.DataSourceId, request.Columns);
            if (error != null) return error;
            if (columns == null || columns.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("Aucune colonne fournie."));

            var cacheKey = BuildCacheKey("recommend-chart", columns);
            if (cache.TryGetValue(cacheKey, out JsonElement cached))
            {
                logger.LogDebug("AI cache hit: recommend-chart ({Key})", cacheKey[^8..]);
                return Ok(ApiResponse<JsonElement>.Ok(cached, new { source = "cache" }));
            }

            var columnsDesc = FormatColumns(columns);
            var prompt = $@"Tu es un expert en visualisation de données.
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
}}";

            var result = await CallGeminiAsync(prompt);
            cache.Set(cacheKey, result, _cacheOpts);
            return Ok(ApiResponse<JsonElement>.Ok(result, new { source = "ai" }));
        }

        // ── POST api/ai/suggest-kpis ──────────────────────────────────────────
        /// <summary>Propose des KPIs pertinents à partir des colonnes du dataset.</summary>
        [HttpPost("suggest-kpis")]
        [ProducesResponseType(typeof(ApiResponse<JsonElement>), 200)]
        public async Task<IActionResult> SuggestKpis([FromBody] AiColumnRequest request)
        {
            var (columns, _, error) = await ResolveColumnsAsync(request.DataSourceId, request.Columns);
            if (error != null) return error;
            if (columns == null || columns.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("Aucune colonne fournie."));

            var cacheKey = BuildCacheKey("suggest-kpis", columns);
            if (cache.TryGetValue(cacheKey, out JsonElement cached))
            {
                logger.LogDebug("AI cache hit: suggest-kpis ({Key})", cacheKey[^8..]);
                return Ok(ApiResponse<JsonElement>.Ok(cached, new { source = "cache" }));
            }

            var columnsDesc = FormatColumns(columns);
            var prompt = $@"Tu es un expert en business intelligence.
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
}}";

            var result = await CallGeminiAsync(prompt);
            cache.Set(cacheKey, result, _cacheOpts);
            return Ok(ApiResponse<JsonElement>.Ok(result, new { source = "ai" }));
        }

        // ── POST api/ai/analyze ───────────────────────────────────────────────
        /// <summary>Génère un résumé textuel : tendances, anomalies, insights automatiques.</summary>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(ApiResponse<JsonElement>), 200)]
        public async Task<IActionResult> Analyze([FromBody] AiAnalyzeRequest request)
        {
            var (columns, resolvedPreview, error) = await ResolveColumnsAsync(
                request.DataSourceId, request.Columns, includePreview: true);
            if (error != null) return error;
            if (columns == null || columns.Count == 0)
                return BadRequest(ApiResponse<object>.Fail("Aucune colonne fournie."));

            var preview = request.Preview ?? resolvedPreview;

            var previewHash = preview != null
                ? ComputeHash(JsonSerializer.Serialize(preview.Take(5)))
                : "no-preview";
            var cacheKey = BuildCacheKey("analyze", columns) + ":" + previewHash;

            if (cache.TryGetValue(cacheKey, out JsonElement cached))
            {
                logger.LogDebug("AI cache hit: analyze ({Key})", cacheKey[^8..]);
                return Ok(ApiResponse<JsonElement>.Ok(cached, new { source = "cache" }));
            }

            var columnsDesc = FormatColumns(columns);
            var previewDesc = preview?.Count > 0
                ? JsonSerializer.Serialize(preview.Take(10))
                : "Non fourni";

            var questionSection = !string.IsNullOrWhiteSpace(request.Question)
                ? $"\nQuestion spécifique : {request.Question}\n"
                : "";

            var prompt = $@"Tu es un data analyst expert.
Voici un dataset avec ses colonnes et un aperçu des données :

Colonnes :
{columnsDesc}

Aperçu (premières lignes) :
{previewDesc}
{questionSection}
Analyse ces données et détecte les tendances, anomalies et insights importants.
Réponds UNIQUEMENT en JSON valide, sans markdown, sans texte avant ou après.
Schéma attendu :
{{
  ""summary"": ""string"",
  ""insights"": [""string""],
  ""trends"": [""string""],
  ""anomalies"": [""string""],
  ""recommendations"": [""string""]
}}";

            var result = await CallGeminiAsync(prompt);
            cache.Set(cacheKey, result, _cacheOpts);
            return Ok(ApiResponse<JsonElement>.Ok(result, new { source = "ai" }));
        }

        // ── POST api/ai/chat ──────────────────────────────────────────────────
        /// <summary>Assistant conversationnel : décrit un graphique en langage naturel.</summary>
        [HttpPost("chat")]
        [ProducesResponseType(typeof(ApiResponse<JsonElement>), 200)]
        public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(ApiResponse<object>.Fail("Message vide."));

            // Chat responses depend on free-text input — cache only when columns provided
            string? cacheKey = null;
            if (request.Columns?.Count > 0)
            {
                var msgHash = ComputeHash(request.Message.Trim().ToLowerInvariant());
                cacheKey = BuildCacheKey("chat", request.Columns) + ":" + msgHash;
                if (cache.TryGetValue(cacheKey, out JsonElement cached))
                {
                    logger.LogDebug("AI cache hit: chat ({Key})", cacheKey[^8..]);
                    return Ok(ApiResponse<JsonElement>.Ok(cached, new { source = "cache" }));
                }
            }

            var columnsSection = request.Columns?.Count > 0
                ? $"Colonnes disponibles :\n{FormatColumns(request.Columns)}"
                : "";

            var prompt = $@"Tu es un assistant pour générateur de tableaux de bord.
L'utilisateur décrit ce qu'il veut visualiser :
""{request.Message}""

{columnsSection}

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
}}";

            var result = await CallGeminiAsync(prompt);
            if (cacheKey != null) cache.Set(cacheKey, result, _cacheOpts);
            return Ok(ApiResponse<JsonElement>.Ok(result, new { source = "ai" }));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<JsonElement> CallGeminiAsync(string prompt)
        {
            var apiKey = config["Gemini:ApiKey"]
                ?? throw new InvalidOperationException("Gemini:ApiKey manquant dans appsettings.");

            var body = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}");
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(req);
            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates))
                throw new InvalidOperationException("Erreur Gemini : " + json[..Math.Min(300, json.Length)]);

            var text = candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? throw new InvalidOperationException("Réponse IA vide.");

            text = text.Replace("```json", "").Replace("```", "").Trim();
            return JsonSerializer.Deserialize<JsonElement>(text);
        }

        private static string BuildCacheKey(string endpoint, List<ColumnDto> columns)
        {
            var input = endpoint + ":" + string.Join("|", columns.Select(c => $"{c.Name}:{c.Type}"));
            return $"ai:{endpoint}:{ComputeHash(input)}";
        }

        private static string ComputeHash(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes)[..16];
        }

        private static string FormatColumns(List<ColumnDto> columns)
            => string.Join("\n", columns.Select(c => $"- {c.Name} (type: {c.Type})"));
    }

    // ── DTOs ──────────────────────────────────────────────────────────────────
    public class AiColumnRequest
    {
        public int? DataSourceId { get; set; }
        public List<ColumnDto> Columns { get; set; } = [];
    }

    public class AiAnalyzeRequest
    {
        public int? DataSourceId { get; set; }
        public string? Question { get; set; }
        public List<ColumnDto> Columns { get; set; } = [];
        public List<Dictionary<string, object?>>? Preview { get; set; }
    }

    public class AiChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public List<ColumnDto>? Columns { get; set; }
    }

    public class ColumnDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
