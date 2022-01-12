using System.Data;
using FSH.WebAPI.Application.Common.Events;
using FSH.WebAPI.Application.Common.Interfaces;
using FSH.WebAPI.Domain.Catalog.Brands;
using FSH.WebAPI.Domain.Catalog.Products;
using FSH.WebAPI.Domain.Common.Contracts;
using FSH.WebAPI.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebAPI.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    private readonly IEventService _eventService;
    public IDbConnection Connection => Database.GetDbConnection();

    public ApplicationDbContext(DbContextOptions options, ICurrentTenant currentTenant, ICurrentUser currentUser, ISerializerService serializer, IEventService eventService)
        : base(options, currentTenant, currentUser, serializer)
    {
        _eventService = eventService;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUser.GetUserId();
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.LastModifiedBy = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = currentUserId;
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedBy = currentUserId;
                        softDelete.DeletedOn = DateTime.UtcNow;
                        entry.State = EntityState.Modified;
                    }

                    break;
            }
        }

        int results = await base.SaveChangesAsync(cancellationToken);
        if (_eventService == null) return results;
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToArray();

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToArray();
            entity.DomainEvents.Clear();
            foreach (var @event in events)
            {
                await _eventService.PublishAsync(@event);
            }
        }

        return results;
    }
}