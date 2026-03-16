using FSH.Framework.Core.Context;
using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Framework.Persistence.Inteceptors;

/// <summary>
/// Interceptor that automatically populates audit metadata for entities implementing <see cref="IAuditableEntity"/>
/// and handles soft delete for entities implementing <see cref="ISoftDeletable"/>.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    [ThreadStatic]
    private static bool _isSaving;

    public AuditableEntitySaveChangesInterceptor(ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (_isSaving)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        try
        {
            _isSaving = true;
            UpdateAuditEntities(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
        finally
        {
            _isSaving = false;
        }
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (_isSaving)
        {
            return base.SavingChanges(eventData, result);
        }

        try
        {
            _isSaving = true;
            UpdateAuditEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void UpdateAuditEntities(DbContext? context)
    {
        if (context == null) return;

        var userId = _currentUser.IsAuthenticated() ? _currentUser.GetUserId().ToString() : null;
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Auditable Entities
            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedOnUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = userId;
                }
                else if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
                {
                    entry.Property(nameof(IAuditableEntity.LastModifiedOnUtc)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.LastModifiedBy)).CurrentValue = userId;
                }
            }

            // Soft Deletable Entities
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(ISoftDeletable.DeletedOnUtc)).CurrentValue = now;
                entry.Property(nameof(ISoftDeletable.DeletedBy)).CurrentValue = userId;
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry != null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
