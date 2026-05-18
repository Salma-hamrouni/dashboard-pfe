using DashboardAPI.Data;
using DashboardAPI.Models;

namespace DashboardAPI.Services
{
    /// <summary>
    /// Enregistre les actions significatives des utilisateurs en base de données.
    /// Injecter en Scoped dans les controllers qui en ont besoin.
    /// </summary>
    public class AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        public async Task LogAsync(
            int    userId,
            string action,
            string entityType,
            int?   entityId  = null,
            string? details  = null,
            string? ipAddress = null)
        {
            var entry = new AuditLog
            {
                UserId     = userId,
                Action     = action,
                EntityType = entityType,
                EntityId   = entityId,
                Details    = details,
                IpAddress  = ipAddress ?? "unknown",
                CreatedAt  = DateTime.UtcNow
            };

            context.AuditLogs.Add(entry);
            await context.SaveChangesAsync();

            logger.LogInformation(
                "[AUDIT] User={UserId} Action={Action} Entity={EntityType}#{EntityId} {Details}",
                userId, action, entityType, entityId, details);
        }
    }
}
