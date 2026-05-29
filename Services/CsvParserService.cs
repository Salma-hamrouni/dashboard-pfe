using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace DashboardAPI.Services
{
    public class ColumnInfo
    {
        public string Name    { get; set; } = string.Empty;
        public string Type    { get; set; } = "string"; // "string" | "number" | "date" | "boolean"
        public int    Index   { get; set; }
    }

    public class CsvParseResult
    {
        public List<ColumnInfo>              Columns  { get; set; } = new();
        public List<Dictionary<string, object?>> Preview  { get; set; } = new(); // 5 premières lignes
        public int                           TotalRows { get; set; }
        public string                        CachedJson { get; set; } = string.Empty;
    }

    public class CsvParserService
    {
        private readonly IWebHostEnvironment _env;

        public CsvParserService(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// Sauvegarde le fichier uploadé et retourne le chemin relatif.
        /// </summary>
        public async Task<string> SaveUploadAsync(IFormFile file)
        {
            var uploads = Path.Combine(_env.ContentRootPath, "Uploads", "csv");
            Directory.CreateDirectory(uploads);

            var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var fullPath = Path.Combine(uploads, safeName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fullPath;
        }

        /// <summary>
        /// Parse le fichier CSV : détecte les colonnes, infère les types, retourne un aperçu
        /// et un snapshot JSON complet pour le cache.
        /// Limite le cache à <see cref="MaxCacheRows"/> lignes pour éviter les crashs mémoire
        /// sur les gros fichiers ; TotalRows reflète le vrai nombre de lignes.
        /// </summary>
        public CsvParseResult Parse(string filePath, char delimiter = ',')
        {
            // Hard cap: prevent OOM on large files. 10k rows × 10 cols × avg 20 chars ≈ 2 MB JSON,
            // well within MySQL max_allowed_packet (default 16–64 MB).
            const int MaxCacheRows = 10_000;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter         = delimiter.ToString(),
                HasHeaderRecord   = true,
                MissingFieldFound = null,
                BadDataFound      = null,
            };

            string[] headers;
            var rawRows   = new List<Dictionary<string, string>>();
            int totalRows = 0;

            // Single pass — collect up to MaxCacheRows, count all rows
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, config))
            {
                csv.Read();
                csv.ReadHeader();
                headers = csv.HeaderRecord ?? Array.Empty<string>();

                while (csv.Read())
                {
                    totalRows++;
                    if (rawRows.Count < MaxCacheRows)
                    {
                        var row = new Dictionary<string, string>(headers.Length);
                        foreach (var h in headers)
                            row[h] = csv.GetField(h) ?? string.Empty;
                        rawRows.Add(row);
                    }
                }
            }

            var columns = headers.Select((h, i) => new ColumnInfo
            {
                Name  = h,
                Index = i,
                Type  = InferType(rawRows.Select(r => r.TryGetValue(h, out var v) ? v : "").ToList())
            }).ToList();

            var typedRows = rawRows.Select(r => CastRow(r, columns)).ToList();

            return new CsvParseResult
            {
                Columns    = columns,
                Preview    = typedRows.Take(5).ToList(),
                TotalRows  = totalRows,
                CachedJson = JsonSerializer.Serialize(new { columns, rows = typedRows })
            };
        }

        // ──────────────────────────────────────────────────────────────────────
        // Helpers privés
        // ──────────────────────────────────────────────────────────────────────

        private static string InferType(List<string> values)
        {
            var nonEmpty = values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            if (nonEmpty.Count == 0) return "string";

            if (nonEmpty.All(v => bool.TryParse(v, out _)))
                return "boolean";

            if (nonEmpty.All(v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out _)))
                return "number";

            if (nonEmpty.All(v => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
                return "date";

            return "string";
        }

        private static Dictionary<string, object?> CastRow(
            Dictionary<string, string> raw,
            List<ColumnInfo> columns)
        {
            var result = new Dictionary<string, object?>();
            foreach (var col in columns)
            {
                var raw_val = raw.TryGetValue(col.Name, out var v) ? v : "";
                result[col.Name] = col.Type switch
                {
                    "number"  => double.TryParse(raw_val, NumberStyles.Any,
                                     CultureInfo.InvariantCulture, out var d) ? d : null,
                    "boolean" => bool.TryParse(raw_val, out var b) ? b : null,
                    "date"    => DateTime.TryParse(raw_val, CultureInfo.InvariantCulture,
                                     DateTimeStyles.None, out var dt) ? dt : null,
                    _         => raw_val
                };
            }
            return result;
        }
    }
}