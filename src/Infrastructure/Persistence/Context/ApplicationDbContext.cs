using System.Data;
using FSH.WebApi.Application.Common.Events;
using FSH.WebApi.Application.Common.Interfaces;
using FSH.WebApi.Domain.Catalog.Brands;
using FSH.WebApi.Domain.Catalog.Products;
using FSH.WebApi.Domain.Common.Contracts;
using FSH.WebApi.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    private readonly IEventService _eventService;

    public ApplicationDbContext(DbContextOptions options, ICurrentTenant currentTenant, ICurrentUser currentUser, ISerializerService serializer, IEventService eventService)
        : base(options, currentTenant, currentUser, serializer)
    {
        _eventService = eventService;
    }

    public IDbConnection Connection => Database.GetDbConnection();

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

        var entitiesWithEvents = ChangeTracker.Entries<IEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToArray();

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToArray();
            entity.DomainEvents.Clear();
            foreach (var domainEvent in domainEvents)
            {
                await _eventService.PublishAsync(domainEvent);
            }
        }

        return results;
    }
}