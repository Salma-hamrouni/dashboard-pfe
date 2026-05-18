using System.Data;
using System.Text.Json;
using MySqlConnector;
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    public class SqlConnectionParams
    {
        public string Server   { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Query    { get; set; } = string.Empty;
        public int    Port     { get; set; } = 3306;
    }

    public class SqlConnectorService
    {
        /// <summary>
        /// Teste la connexion sans exécuter de requête.
        /// Retourne true si la connexion réussit.
        /// </summary>
        public async Task<bool> TestConnectionAsync(SqlConnectionParams p)
        {
            try
            {
                await using var conn = BuildConnection(p);
                await conn.OpenAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Mots-clés DDL/DML interdits pour éviter l'injection
        private static readonly string[] ForbiddenKeywords =
        [
            "DROP", "DELETE", "INSERT", "UPDATE", "TRUNCATE",
            "ALTER", "CREATE", "EXEC", "EXECUTE", "MERGE",
            "CALL", "REPLACE", "LOAD", "OUTFILE", "DUMPFILE",
            "--", "/*", "*/", "xp_", "sp_"
        ];

        /// <summary>
        /// Exécute la requête SQL et retourne colonnes + lignes + cache JSON.
        /// </summary>
        public async Task<ConnectorResult> FetchDataAsync(SqlConnectionParams p)
        {
            await using var conn = BuildConnection(p);
            await conn.OpenAsync();

            // Protection renforcée : SELECT uniquement + aucun mot-clé dangereux
            var trimmed = p.Query.TrimStart().ToUpperInvariant();
            if (!trimmed.StartsWith("SELECT ") && !trimmed.StartsWith("SELECT\t"))
                throw new InvalidOperationException("Seules les requêtes SELECT sont autorisées.");

            if (ForbiddenKeywords.Any(kw => trimmed.Contains(kw)))
                throw new InvalidOperationException(
                    "La requête contient des opérations non autorisées.");

            await using var cmd    = new MySqlCommand(p.Query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var columns = new List<ColumnInfo>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(new ColumnInfo
                {
                    Name  = reader.GetName(i),
                    Index = i,
                    Type  = MapDbType(reader.GetFieldType(i))
                });
            }

            var rows = new List<Dictionary<string, object?>>();
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                foreach (var col in columns)
                    row[col.Name] = reader.IsDBNull(col.Index) ? null : reader.GetValue(col.Index);
                rows.Add(row);
            }

            return new ConnectorResult
            {
                Columns    = columns,
                Rows       = rows,
                TotalRows  = rows.Count,
                CachedJson = JsonSerializer.Serialize(new { columns, rows })
            };
        }

        // ── Helpers privés ────────────────────────────────────────────────────
        private static MySqlConnection BuildConnection(SqlConnectionParams p)
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server   = p.Server,
                Port     = (uint)p.Port,
                Database = p.Database,
                UserID   = p.Username,
                Password = p.Password,
                ConnectionTimeout = 10
            };
            return new MySqlConnection(csb.ConnectionString);
        }

        private static string MapDbType(Type t)
        {
            if (t == typeof(int) || t == typeof(long) ||
                t == typeof(float) || t == typeof(double) || t == typeof(decimal))
                return "number";
            if (t == typeof(DateTime) || t == typeof(DateOnly))
                return "date";
            if (t == typeof(bool))
                return "boolean";
            return "string";
        }
    }
}
