using System.Text;
using System.Text.Json;
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    public class RestConnectionParams
    {
        public string              Endpoint { get; set; } = string.Empty;
        public string              Method   { get; set; } = "GET";
        public Dictionary<string, string> Headers  { get; set; } = new();
        public string?             Body     { get; set; }
        /// <summary>
        /// Chemin JSON vers le tableau de données dans la réponse.
        /// Ex: "data.items" pour { "data": { "items": [...] } }
        /// Laisser vide si la réponse est directement un tableau.
        /// </summary>
        public string? DataPath { get; set; }
    }

    public class ConnectorResult
    {
        public List<ColumnInfo>              Columns   { get; set; } = new();
        public List<Dictionary<string, object?>> Rows  { get; set; } = new();
        public int                           TotalRows { get; set; }
        public string                        CachedJson { get; set; } = string.Empty;
    }

    public class RestConnectorService
    {
        private readonly HttpClient _http;

        public RestConnectorService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Appelle l'API REST et retourne colonnes + lignes + cache JSON.
        /// </summary>
        public async Task<ConnectorResult> FetchDataAsync(RestConnectionParams p)
        {
            using var request = new HttpRequestMessage(
                new HttpMethod(p.Method.ToUpperInvariant()),
                p.Endpoint
            );

            // Injecter les headers personnalisés (ex: Authorization: Bearer ...)
            foreach (var (key, value) in p.Headers)
                request.Headers.TryAddWithoutValidation(key, value);

            // Body pour POST/PUT
            if (!string.IsNullOrEmpty(p.Body) && p.Method.ToUpperInvariant() != "GET")
                request.Content = new StringContent(p.Body, Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            // Naviguer jusqu'au tableau de données via DataPath
            var dataElement = ResolveDataPath(doc.RootElement, p.DataPath);

            if (dataElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException(
                    $"La réponse API n'est pas un tableau JSON. ValueKind: {dataElement.ValueKind}. " +
                    "Vérifiez le champ DataPath.");

            var rows = new List<Dictionary<string, object?>>();
            foreach (var item in dataElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;
                var row = new Dictionary<string, object?>();
                foreach (var prop in item.EnumerateObject())
                    row[prop.Name] = ExtractValue(prop.Value);
                rows.Add(row);
            }

            // Inférer les colonnes depuis la première ligne
            var columns = rows.FirstOrDefault()?.Keys.Select((k, i) => new ColumnInfo
            {
                Name  = k,
                Index = i,
                Type  = InferType(rows.Select(r =>
                    r.TryGetValue(k, out var v) ? v?.ToString() ?? "" : "").ToList())
            }).ToList() ?? new List<ColumnInfo>();

            return new ConnectorResult
            {
                Columns    = columns,
                Rows       = rows,
                TotalRows  = rows.Count,
                CachedJson = JsonSerializer.Serialize(new { columns, rows })
            };
        }

        // ── Helpers privés ────────────────────────────────────────────────────
        private static JsonElement ResolveDataPath(JsonElement root, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return root;

            var current = root;
            foreach (var key in path.Split('.'))
            {
                if (current.ValueKind != JsonValueKind.Object ||
                    !current.TryGetProperty(key, out current))
                    throw new InvalidOperationException($"DataPath invalide : clé '{key}' introuvable.");
            }
            return current;
        }

        private static object? ExtractValue(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.Number  => el.TryGetDouble(out var d) ? d : null,
            JsonValueKind.True    => true,
            JsonValueKind.False   => false,
            JsonValueKind.Null    => null,
            JsonValueKind.String  => el.GetString(),
            _                     => el.GetRawText()
        };

        private static string InferType(List<string> values)
        {
            var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            if (nonEmpty.Count == 0) return "string";
            if (nonEmpty.All(v => double.TryParse(v, out _)))  return "number";
            if (nonEmpty.All(v => DateTime.TryParse(v, out _))) return "date";
            if (nonEmpty.All(v => bool.TryParse(v, out _)))    return "boolean";
            return "string";
        }
    }
}