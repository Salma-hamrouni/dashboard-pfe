using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using DashboardAPI.Models;
using DashboardAPI.Services;

namespace DashboardAPI.Services
{
    public class SqlServerConnectionParams
    {
        public string Server   { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Query    { get; set; } = string.Empty;
        public int    Port     { get; set; } = 1433;
        /// <summary>
        /// Si true : utilise l'authentification Windows (Integrated Security).
        /// Si false : utilise Username + Password.
        /// </summary>
        public bool   IntegratedSecurity { get; set; } = false;
    }

    public class SqlServerConnectorService
    {
        /// <summary>
        /// Teste la connexion SQL Server sans exécuter de requête.
        /// </summary>
        public async Task<bool> TestConnectionAsync(SqlServerConnectionParams p)
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

        /// <summary>
        /// Exécute la requête SQL Server et retourne colonnes + lignes + cache JSON.
        /// </summary>
        public async Task<ConnectorResult> FetchDataAsync(SqlServerConnectionParams p)
        {
            // Sécurité : SELECT uniquement
            var trimmed = p.Query.TrimStart().ToUpperInvariant();
            if (!trimmed.StartsWith("SELECT"))
                throw new InvalidOperationException("Seules les requêtes SELECT sont autorisées.");

            await using var conn = BuildConnection(p);
            await conn.OpenAsync();

            await using var cmd    = new SqlCommand(p.Query, conn);
            cmd.CommandTimeout = 30;

            await using var reader = await cmd.ExecuteReaderAsync();

            // Lire les colonnes
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

            // Lire les lignes
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

        // ── Helpers privés ────────────────────────────────────────────
        private static SqlConnection BuildConnection(SqlServerConnectionParams p)
        {
            var csb = new SqlConnectionStringBuilder
            {
                DataSource         = p.Port != 1433
                    ? $"{p.Server},{p.Port}"
                    : p.Server,
                InitialCatalog     = p.Database,
                ConnectTimeout     = 10,
                TrustServerCertificate = true  // utile pour les environnements de dev
            };

            if (p.IntegratedSecurity)
            {
                csb.IntegratedSecurity = true;
            }
            else
            {
                csb.UserID   = p.Username;
                csb.Password = p.Password;
            }

            return new SqlConnection(csb.ConnectionString);
        }

        private static string MapDbType(Type t)
        {
            if (t == typeof(int)    || t == typeof(long)    ||
                t == typeof(float)  || t == typeof(double)  ||
                t == typeof(decimal)|| t == typeof(short))
                return "number";
            if (t == typeof(DateTime) || t == typeof(DateOnly))
                return "date";
            if (t == typeof(bool))
                return "boolean";
            return "string";
        }
    }
}
