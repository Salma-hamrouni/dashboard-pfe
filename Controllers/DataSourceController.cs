using DashboardAPI.Data;
using DashboardAPI.Models;
using DashboardAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace DashboardAPI.Controllers
{
    [Route("api/datasource")]
    public class DataSourceController : BaseController
    {
        private readonly DataSourceDao       _dao;
        private readonly CsvParserService    _csv;
        private readonly SqlConnectorService _sql;
        private readonly RestConnectorService _rest;

        public DataSourceController(
            DataSourceDao        dao,
            CsvParserService     csv,
            SqlConnectorService  sql,
            RestConnectorService rest)
        {
            _dao  = dao;
            _csv  = csv;
            _sql  = sql;
            _rest = rest;
        }

        private static readonly HashSet<string> _allowedMimeTypes =
        [
            "text/csv",
            "text/plain",
            "text/tab-separated-values",
            "application/csv",
            "application/vnd.ms-excel" // Excel sometimes sends this for .csv
        ];

        // Known binary file magic bytes — CSV must NOT start with these
        private static readonly (byte[] Magic, string Label)[] _binarySignatures =
        [
            (new byte[] { 0x50, 0x4B }              , "ZIP/Office"),
            (new byte[] { 0xFF, 0xD8, 0xFF }        , "JPEG"),
            (new byte[] { 0x89, 0x50, 0x4E, 0x47 }  , "PNG"),
            (new byte[] { 0x25, 0x50, 0x44, 0x46 }  , "PDF"),
            (new byte[] { 0x4D, 0x5A }              , "EXE/PE"),
            (new byte[] { 0x7F, 0x45, 0x4C, 0x46 }  , "ELF"),
            (new byte[] { 0x47, 0x49, 0x46 }        , "GIF"),
            (new byte[] { 0x42, 0x4D }              , "BMP"),
            (new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }  , "OLE/Office"),
        ];

        private static async Task<string?> ValidateMagicBytesAsync(IFormFile file)
        {
            var buffer = new byte[8];
            using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
            if (read == 0) return "Le fichier est vide.";

            foreach (var (magic, label) in _binarySignatures)
            {
                if (read >= magic.Length && buffer.AsSpan(0, magic.Length).SequenceEqual(magic))
                    return $"Fichier binaire détecté ({label}). Seuls les fichiers texte CSV/TSV sont acceptés.";
            }

            // Reject files with more than 5% non-printable bytes in the first 512 bytes
            using var fullStream = file.OpenReadStream();
            var sample = new byte[512];
            var sampleRead = await fullStream.ReadAsync(sample.AsMemory(0, sample.Length));
            var nonPrintable = sample.AsSpan(0, sampleRead)
                .ToArray()
                .Count(b => b < 0x09 || (b > 0x0D && b < 0x20));
            if (sampleRead > 0 && (double)nonPrintable / sampleRead > 0.05)
                return "Le fichier contient des caractères binaires non autorisés.";

            return null;
        }

        // ── GET api/datasource ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _dao.GetAllByUserAsync(GetUserId()));

        // ── GET api/datasource/{id} ───────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var ds = await _dao.GetByIdAsync(id);
            if (ds == null || ds.UserId != GetUserId()) return NotFound();
            return Ok(ds);
        }

        // ── POST api/datasource/upload-csv ────────────────────────────────────
        [HttpPost("upload-csv")]
        [EnableRateLimiting("upload-policy")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
        public async Task<IActionResult> UploadCsv(
            IFormFile file,
            [FromForm] string name,
            [FromForm] string? description,
            [FromForm] char delimiter = ',')
        {
            if (file == null || file.Length == 0)
                return BadRequest("Aucun fichier fourni.");

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Le fichier dépasse la taille maximale autorisée (10 Mo).");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".csv" && ext != ".tsv")
                return BadRequest("Seuls les fichiers .csv et .tsv sont acceptés.");

            // Validate MIME type declared by client
            var contentType = file.ContentType.ToLowerInvariant();
            if (!_allowedMimeTypes.Contains(contentType))
                return BadRequest($"Type MIME non autorisé : {file.ContentType}");

            // Validate magic bytes — reject binary files masquerading as CSV
            var magicError = await ValidateMagicBytesAsync(file);
            if (magicError != null)
                return BadRequest(magicError);

            var filePath    = await _csv.SaveUploadAsync(file);
            var parseResult = _csv.Parse(filePath, delimiter);

            var ds = new DataSource
            {
                Name        = name,
                Description = description,
                Type        = DataSourceType.CSV,
                FilePath    = filePath,
                CachedDataJson      = parseResult.CachedJson,
                LastRefreshedAt     = DateTime.UtcNow,
                ConnectionParamsJson = JsonSerializer.Serialize(new
                {
                    fileName  = file.FileName,
                    delimiter = delimiter.ToString()
                }),
                UserId = GetUserId()
            };

            var created = await _dao.CreateAsync(ds);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new
            {
                created.Id,
                created.Name,
                created.Type,
                Columns   = parseResult.Columns,
                Preview   = parseResult.Preview,
                TotalRows = parseResult.TotalRows
            });
        }

        // ── POST api/datasource/connect-sql ──────────────────────────────────
        [HttpPost("connect-sql")]
        public async Task<IActionResult> ConnectSql([FromBody] ConnectSqlRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Le nom est obligatoire.");

            var p = new SqlConnectionParams
            {
                Server   = request.Server,
                Database = request.Database,
                Username = request.Username,
                Password = request.Password,
                Query    = request.Query,
                Port     = request.Port
            };

            // Test connexion d'abord
            var ok = await _sql.TestConnectionAsync(p);
            if (!ok) return BadRequest("Impossible de se connecter au serveur SQL. Vérifiez les paramètres.");

            // Récupérer les données
            var result = await _sql.FetchDataAsync(p);

            var ds = new DataSource
            {
                Name        = request.Name,
                Description = request.Description,
                Type        = DataSourceType.SQL,
                CachedDataJson      = result.CachedJson,
                LastRefreshedAt     = DateTime.UtcNow,
                ConnectionParamsJson = JsonSerializer.Serialize(p),
                UserId = GetUserId()
            };

            var created = await _dao.CreateAsync(ds);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new
            {
                created.Id,
                created.Name,
                created.Type,
                result.Columns,
                Preview   = result.Rows.Take(5),
                result.TotalRows
            });
        }

        // ── POST api/datasource/connect-rest ─────────────────────────────────
        [HttpPost("connect-rest")]
        public async Task<IActionResult> ConnectRest([FromBody] ConnectRestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Le nom est obligatoire.");

            var p = new RestConnectionParams
            {
                Endpoint = request.Endpoint,
                Method   = request.Method,
                Headers  = request.Headers ?? new Dictionary<string, string>(),
                Body     = request.Body,
                DataPath = request.DataPath
            };

            ConnectorResult result;
            try
            {
                result = await _rest.FetchDataAsync(p);
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur lors de l'appel REST : {ex.Message}");
            }

            var ds = new DataSource
            {
                Name        = request.Name,
                Description = request.Description,
                Type        = DataSourceType.REST,
                CachedDataJson      = result.CachedJson,
                LastRefreshedAt     = DateTime.UtcNow,
                ConnectionParamsJson = JsonSerializer.Serialize(p),
                UserId = GetUserId()
            };

            var created = await _dao.CreateAsync(ds);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, new
            {
                created.Id,
                created.Name,
                created.Type,
                result.Columns,
                Preview   = result.Rows.Take(5),
                result.TotalRows
            });
        }

        // ── POST api/datasource/{id}/refresh ─────────────────────────────────
        [HttpPost("{id:int}/refresh")]
        public async Task<IActionResult> Refresh(int id)
        {
            var ds = await _dao.GetByIdAsync(id);
            if (ds == null || ds.UserId != GetUserId()) return NotFound();

            if (string.IsNullOrEmpty(ds.ConnectionParamsJson))
                return BadRequest("Aucun paramètre de connexion enregistré.");

            ConnectorResult result;

            switch (ds.Type)
            {
                case DataSourceType.CSV:
                    // Re-parser le fichier CSV existant
                    if (string.IsNullOrEmpty(ds.FilePath) || !System.IO.File.Exists(ds.FilePath))
                        return BadRequest("Fichier CSV introuvable sur le serveur.");
                    var csvResult = _csv.Parse(ds.FilePath);
                    result = new ConnectorResult
                    {
                        Columns    = csvResult.Columns,
                        Rows       = new List<Dictionary<string, object?>>(),
                        TotalRows  = csvResult.TotalRows,
                        CachedJson = csvResult.CachedJson
                    };
                    break;

                case DataSourceType.SQL:
                    var sqlParams = JsonSerializer.Deserialize<SqlConnectionParams>(
                        ds.ConnectionParamsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    result = await _sql.FetchDataAsync(sqlParams);
                    break;

                case DataSourceType.REST:
                    var restParams = JsonSerializer.Deserialize<RestConnectionParams>(
                        ds.ConnectionParamsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
                    result = await _rest.FetchDataAsync(restParams);
                    break;

                default:
                    return BadRequest("Type de source non supporté.");
            }

            ds.CachedDataJson   = result.CachedJson;
            ds.LastRefreshedAt  = DateTime.UtcNow;
            await _dao.UpdateAsync(ds);

            return Ok(new
            {
                ds.Id,
                ds.Name,
                ds.Type,
                ds.LastRefreshedAt,
                result.Columns,
                result.TotalRows
            });
        }

        // ── POST api/datasource/{id}/preview ─────────────────────────────────
        [HttpPost("{id:int}/preview")]
        public async Task<IActionResult> Preview(int id)
        {
            var ds = await _dao.GetByIdAsync(id);
            if (ds == null || ds.UserId != GetUserId()) return NotFound();

            if (string.IsNullOrEmpty(ds.CachedDataJson))
                return BadRequest("Aucune donnée en cache. Effectuez d'abord un refresh.");

            using var doc  = JsonDocument.Parse(ds.CachedDataJson);
            var root = doc.RootElement;

            return Ok(new
            {
                dataSourceId  = ds.Id,
                name          = ds.Name,
                type          = ds.Type.ToString(),
                lastRefreshed = ds.LastRefreshedAt,
                columns       = root.TryGetProperty("columns", out var cols) ? cols : default,
                preview       = root.TryGetProperty("rows", out var rows)
                                    ? rows.EnumerateArray().Take(5).ToArray()
                                    : Array.Empty<JsonElement>()
            });
        }

        // ── DELETE api/datasource/{id} ────────────────────────────────────────
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ds = await _dao.GetByIdAsync(id);
            if (ds == null || ds.UserId != GetUserId()) return NotFound();

            if (ds.Type == DataSourceType.CSV
                && !string.IsNullOrEmpty(ds.FilePath)
                && System.IO.File.Exists(ds.FilePath))
                System.IO.File.Delete(ds.FilePath);

            await _dao.DeleteAsync(id);
            return NoContent();
        }
    }

    // ── DTOs des requêtes ─────────────────────────────────────────────────────
    public record ConnectSqlRequest(
        string  Name,
        string? Description,
        string  Server,
        string  Database,
        string  Username,
        string  Password,
        string  Query,
        int     Port = 3306
    );

    public record ConnectRestRequest(
        string  Name,
        string? Description,
        string  Endpoint,
        string  Method,
        Dictionary<string, string>? Headers,
        string? Body,
        string? DataPath
    );
}