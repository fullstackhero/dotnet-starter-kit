using FSH.Framework.Core.Context;
using FSH.Framework.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Framework.Persistence.Inteceptors;

/// <summary>
/// Interceptor that automatically populates audit metadata for entities implementing <see cref="IAuditableEntity"/>
/// and handles soft delete for entities implementing <see cref="ISoftDeletable"/>.
/// Uses an <see cref="AsyncLocal{T}"/> recursion guard to prevent StackOverflowException from nested SaveChanges calls.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    private static readonly AsyncLocal<bool> _isSaving = new();

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
        ArgumentNullException.ThrowIfNull(eventData);

        if (_isSaving.Value)
        {
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            _isSaving.Value = true;
            UpdateAuditEntities(eventData.Context);
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _isSaving.Value = false;
        }
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (_isSaving.Value)
        {
            return base.SavingChanges(eventData, result);
        }

        try
        {
            _isSaving.Value = true;
            UpdateAuditEntities(eventData.Context);
            return base.SavingChanges(eventData, result);
        }
        finally
        {
            _isSaving.Value = false;
        }
    }

    private void UpdateAuditEntities(DbContext? context)
    {
        if (context is null) return;

        var userId = _currentUser.IsAuthenticated() ? _currentUser.GetUserId().ToString() : null;
        var now = _timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditableEntity)
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

            if (entry.Entity is ISoftDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
                entry.Property(nameof(ISoftDeletable.DeletedOnUtc)).CurrentValue = now;
                entry.Property(nameof(ISoftDeletable.DeletedBy)).CurrentValue = userId;

                // When EF marks the parent for deletion it cascades the
                // EntityState.Deleted onto every owned reference. Flipping
                // the parent back to Modified isn't enough — without
                // restoring the owned references they'd be NULLed out in
                // the generated UPDATE (we saw this on Product.Price /
                // Money, where the soft delete produced a NOT NULL
                // violation on PriceAmount).
                foreach (var reference in entry.References)
                {
                    if (reference.TargetEntry is { } target
                        && target.Metadata.IsOwned()
                        && target.State == EntityState.Deleted)
                    {
                        target.State = EntityState.Unchanged;
                    }
                }
            }
        }
    }
}

internal static class EntityEntryExtensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
        entry.References.Any(r =>
            r.TargetEntry is not null &&
            r.TargetEntry.Metadata.IsOwned() &&
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}