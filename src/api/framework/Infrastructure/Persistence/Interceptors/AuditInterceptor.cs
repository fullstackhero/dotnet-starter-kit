using System.Collections.ObjectModel;
using FSH.Framework.Core.Audit;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Framework.Core.Identity.Users.Abstractions;
using FSH.Framework.Infrastructure.Identity.Audit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Framework.Infrastructure.Persistence.Interceptors;
public class AuditInterceptor(ICurrentUser currentUser, TimeProvider timeProvider, IPublisher publisher) : SaveChangesInterceptor
{

    public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        await PublishAuditTrailsAsync(eventData);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task PublishAuditTrailsAsync(DbContextEventData eventData)
    {
        if (eventData.Context == null) return;
        eventData.Context.ChangeTracker.DetectChanges();
        var trails = new List<TrailDto>();
        var utcNow = timeProvider.GetUtcNow();
        foreach (var entry in eventData.Context.ChangeTracker.Entries<IAuditable>().Where(x => x.State is EntityState.Added or EntityState.Deleted or EntityState.Modified).ToList())
        {
            var userId = currentUser.GetUserId();
            var trail = new TrailDto()
            {
                Id = Guid.NewGuid(),
                TableName = entry.Entity.GetType().Name,
                UserId = userId,
                DateTime = utcNow
            };

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    continue;
                }
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    trail.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        trail.Type = TrailType.Create;
                        trail.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        trail.Type = TrailType.Delete;
                        trail.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            if (entry.Entity is ISoftDeletable && property.OriginalValue == null && property.CurrentValue != null)
                            {
                                trail.ModifiedProperties.Add(propertyName);
                                trail.Type = TrailType.Delete;
                                trail.OldValues[propertyName] = property.OriginalValue;
                                trail.NewValues[propertyName] = property.CurrentValue;
                            }
                            else if (property.OriginalValue?.Equals(property.CurrentValue) == false)
                            {
                                trail.ModifiedProperties.Add(propertyName);
                                trail.Type = TrailType.Update;
                                trail.OldValues[propertyName] = property.OriginalValue;
                                trail.NewValues[propertyName] = property.CurrentValue;
                            }
                            else
                            {
                                property.IsModified = false;
                            }
                        }
                        break;
                }
            }

            trails.Add(trail);
        }
        if (trails.Count == 0) return;
        var auditTrails = new Collection<AuditTrail>();
        foreach (var trail in trails)
        {
            auditTrails.Add(trail.ToAuditTrail());
        }
        await publisher.Publish(new AuditPublishedEvent(auditTrails));
    }

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            var utcNow = timeProvider.GetUtcNow();
            if (entry.State is EntityState.Added or EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = currentUser.GetUserId();
                    entry.Entity.Created = utcNow;
                }
                entry.Entity.LastModifiedBy = currentUser.GetUserId();
                entry.Entity.LastModified = utcNow;
            }
            if(entry.State is EntityState.Deleted && entry.Entity is ISoftDeletable softDelete)
            {
                softDelete.DeletedBy = currentUser.GetUserId();
                softDelete.Deleted = utcNow;
                entry.State = EntityState.Modified;
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
