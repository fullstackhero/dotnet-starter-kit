using FSH.Framework.Auditing.Contracts.Events.IntegrationEvents;
using FSH.Framework.Auditing.Data;
using FSH.Framework.Core.Messaging.Events;
using FSH.Modules.Auditing.Core.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace FSH.Framework.Auditing.EventHandlers;

[SuppressMessage("Performance", "CA1848")]
[SuppressMessage("Design", "CA1031")]
public class AuditPublishedIntegrationEventHandler(
    ILogger<AuditPublishedIntegrationEventHandler> logger,
    IAuditingDbContext context)
    : IEventHandler<AuditPublishedEvent>
{
    public async Task HandleAsync(AuditPublishedEvent notification, CancellationToken cancellationToken = default)
    {
        if (notification.Trails == null || notification.Trails.Count == 0)
        {
            logger.LogDebug("No audit trails to persist.");
            return;
        }
        try
        {
            var trailEntities = notification.Trails.ToEntityList();
            await context.Trails.AddRangeAsync(trailEntities, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Persisted {Count} audit trail(s).", notification.Trails.Count);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Database update error while saving audit trails.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while saving audit trails.");
        }
    }
}