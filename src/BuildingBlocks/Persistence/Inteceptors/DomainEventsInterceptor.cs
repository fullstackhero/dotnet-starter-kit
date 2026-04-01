using FSH.Framework.Core.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Persistence.Inteceptors;

/// <summary>
/// Entity Framework interceptor that automatically publishes domain events after saving changes.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DomainEventsInterceptor"/> class.
/// </remarks>
/// <param name="publisher">The mediator publisher for publishing domain events.</param>
/// <param name="logger">Logger for tracking domain event publication.</param>
public sealed class DomainEventsInterceptor(IPublisher publisher, ILogger<DomainEventsInterceptor> logger) : SaveChangesInterceptor
{

    /// <summary>
    /// Called before changes are saved to the database.
    /// </summary>
    /// <param name="eventData">Contextual information about the DbContext being saved.</param>
    /// <param name="result">The result to be returned from SaveChanges.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Called after changes have been saved to the database. Publishes all domain events from tracked entities.
    /// </summary>
    /// <param name="eventData">Contextual information about the completed save operation.</param>
    /// <param name="result">The number of state entries written to the database.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventData is null.</exception>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context == null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var domainEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(e =>
            {
                var pending = e.Entity.DomainEvents.ToArray();
                e.Entity.ClearDomainEvents();
                return pending;
            })
            .ToArray();

        if (domainEvents.Length == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Publishing {Count} domain events...", domainEvents.Length);
        }

        foreach (var domainEvent in domainEvents)
            await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}