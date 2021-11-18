using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using DN.WebApi.Domain.Contracts;
using DN.WebApi.Domain.Entities.Catalog;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence
{
    public class ApplicationDbContext : BaseDbContext
    {
        private readonly IEventService _eventService;
        private readonly ISerializerService _serializer;
        public IDbConnection Connection => Database.GetDbConnection();
        private readonly ICurrentUser _currentUserService;
        private readonly ITenantService _tenantService;

        public ApplicationDbContext(DbContextOptions options, ITenantService tenantService, ICurrentUser currentUserService, ISerializerService serializer, IEventService eventService)
        : base(options, tenantService, currentUserService, serializer)
        {
            _tenantService = tenantService;
            _currentUserService = currentUserService;
            _serializer = serializer;
            _eventService = eventService;
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Brand> Brands { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var currentUserId = _currentUserService.GetUserId();
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
}