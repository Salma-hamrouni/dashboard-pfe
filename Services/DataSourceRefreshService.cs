using DashboardAPI.Data;
using DashboardAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DashboardAPI.Services
{
    /// <summary>
    /// Background service : rafraîchit automatiquement les DataSources SQL et REST
    /// dont le cache est plus vieux que RefreshIntervalMinutes.
    /// Tourne en arrière-plan dès le démarrage de l'API.
    /// </summary>
    public class DataSourceRefreshService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<DataSourceRefreshService> logger) : BackgroundService
    {
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(
            config.GetValue("DataSourceRefresh:IntervalMinutes", 30));

        private readonly TimeSpan _staleThreshold = TimeSpan.FromMinutes(
            config.GetValue("DataSourceRefresh:StaleThresholdMinutes", 60));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation(
                "DataSourceRefreshService démarré — intervalle: {Interval}min, seuil: {Threshold}min",
                _interval.TotalMinutes, _staleThreshold.TotalMinutes);

            // Attendre le démarrage complet de l'app avant le premier cycle
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RefreshStaleSources(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Erreur inattendue dans DataSourceRefreshService");
                }

                await Task.Delay(_interval, stoppingToken);
            }

            logger.LogInformation("DataSourceRefreshService arrêté");
        }

        private async Task RefreshStaleSources(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sql     = scope.ServiceProvider.GetRequiredService<SqlConnectorService>();
            var rest    = scope.ServiceProvider.GetRequiredService<RestConnectorService>();

            var threshold = DateTime.UtcNow.Subtract(_staleThreshold);

            // Uniquement SQL et REST (CSV n'a pas de reconnexion automatique utile)
            var staleSources = await context.DataSources
                .Where(ds => ds.Type != DataSourceType.CSV
                          && (ds.LastRefreshedAt == null || ds.LastRefreshedAt < threshold)
                          && ds.ConnectionParamsJson != null)
                .AsNoTracking()
                .ToListAsync(ct);

            if (staleSources.Count == 0)
            {
                logger.LogDebug("DataSourceRefresh — aucune source obsolète");
                return;
            }

            logger.LogInformation(
                "DataSourceRefresh — {Count} source(s) à rafraîchir", staleSources.Count);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            int success = 0, failed = 0;

            foreach (var ds in staleSources)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    ConnectorResult result = ds.Type switch
                    {
                        DataSourceType.SQL => await RefreshSql(ds, sql, opts),
                        DataSourceType.REST => await RefreshRest(ds, rest, opts),
                        _ => throw new InvalidOperationException($"Type {ds.Type} non supporté")
                    };

                    // Mise à jour directe sans charger l'entité (update ciblé)
                    await context.DataSources
                        .Where(d => d.Id == ds.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(d => d.CachedDataJson,  result.CachedJson)
                            .SetProperty(d => d.LastRefreshedAt, DateTime.UtcNow),
                        ct);

                    success++;
                    logger.LogInformation(
                        "DataSource {Id} ({Name}) rafraîchie — {Rows} lignes",
                        ds.Id, ds.Name, result.TotalRows);
                }
                catch (Exception ex)
                {
                    failed++;
                    logger.LogWarning(ex,
                        "DataSource {Id} ({Name}) — échec refresh: {Error}",
                        ds.Id, ds.Name, ex.Message);
                }
            }

            logger.LogInformation(
                "DataSourceRefresh terminé — {Success} OK, {Failed} échoués",
                success, failed);
        }

        private static async Task<ConnectorResult> RefreshSql(
            DataSource ds, SqlConnectorService sql, JsonSerializerOptions opts)
        {
            var p = JsonSerializer.Deserialize<SqlConnectionParams>(
                ds.ConnectionParamsJson!, opts)
                ?? throw new InvalidOperationException("Params SQL invalides");
            return await sql.FetchDataAsync(p);
        }

        private static async Task<ConnectorResult> RefreshRest(
            DataSource ds, RestConnectorService rest, JsonSerializerOptions opts)
        {
            var p = JsonSerializer.Deserialize<RestConnectionParams>(
                ds.ConnectionParamsJson!, opts)
                ?? throw new InvalidOperationException("Params REST invalides");
            return await rest.FetchDataAsync(p);
        }
    }
}
