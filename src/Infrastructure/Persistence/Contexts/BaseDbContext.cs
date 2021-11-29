using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Application.Multitenancy;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Contracts;
using DN.WebApi.Infrastructure.Auditing;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Persistence.Contexts;

public abstract class BaseDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, ApplicationRoleClaim, IdentityUserToken<string>>
{
    private readonly ISerializerService _serializer;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUser _currentUserService;
    public DbSet<Trail> AuditTrails { get; set; }
    public string Tenant { get; set; }

    protected BaseDbContext(DbContextOptions options, ITenantService tenantService, ICurrentUser currentUserService, ISerializerService serializer)
    : base(options)
    {
        _tenantService = tenantService;
        Tenant = _tenantService?.GetCurrentTenant()?.Key;
        _currentUserService = currentUserService;
        _serializer = serializer;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        modelBuilder.ApplyIdentityConfiguration(_tenantService);
        modelBuilder.ApplyGlobalFilters<IMustHaveTenant>(b => EF.Property<string>(b, nameof(Tenant)) == Tenant);
        modelBuilder.ApplyGlobalFilters<IIdentityTenant>(b => EF.Property<string>(b, nameof(Tenant)) == Tenant);
        modelBuilder.ApplyGlobalFilters<ISoftDelete>(s => s.DeletedOn == null);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.EnableSensitiveDataLogging();

        string tenantConnectionString = _tenantService.GetConnectionString();
        if (!string.IsNullOrEmpty(tenantConnectionString))
        {
            string dbProvider = _tenantService.GetDatabaseProvider();
            switch (dbProvider.ToLower())
            {
                case "postgresql":
                    optionsBuilder.UseNpgsql(tenantConnectionString);
                    break;

                case "mssql":
                    optionsBuilder.UseSqlServer(tenantConnectionString);
                    break;

                case "mysql":
                    optionsBuilder.UseMySql(tenantConnectionString, ServerVersion.AutoDetect(tenantConnectionString));
                    break;
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.Tenant = Tenant;
                    break;
            }
        }

        var currentUserId = _currentUserService.GetUserId();
        var auditEntries = OnBeforeSaveChanges(currentUserId);
        int result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChangesAsync(auditEntries, cancellationToken);
        return result;
    }

    private List<AuditTrail> OnBeforeSaveChanges(in Guid userId)
    {
        ChangeTracker.DetectChanges();
        var trailEntries = new List<AuditTrail>();
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .ToList())
        {
            var trailEntry = new AuditTrail(entry, _serializer)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId
            };
            trailEntries.Add(trailEntry);
            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    trailEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    trailEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        trailEntry.TrailType = TrailType.Create;
                        trailEntry.NewValues[propertyName] = property.CurrentValue;
                        break;

                    case EntityState.Deleted:
                        trailEntry.TrailType = TrailType.Delete;
                        trailEntry.OldValues[propertyName] = property.OriginalValue;
                        break;

                    case EntityState.Modified:
                        if (property.IsModified && entry.Entity is ISoftDelete && property.OriginalValue == null && property.CurrentValue != null)
                        {
                            trailEntry.ChangedColumns.Add(propertyName);
                            trailEntry.TrailType = TrailType.Delete;
                            trailEntry.OldValues[propertyName] = property.OriginalValue;
                            trailEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        else if (property.IsModified && property.OriginalValue?.Equals(property.CurrentValue) == false)
                        {
                            trailEntry.ChangedColumns.Add(propertyName);
                            trailEntry.TrailType = TrailType.Update;
                            trailEntry.OldValues[propertyName] = property.OriginalValue;
                            trailEntry.NewValues[propertyName] = property.CurrentValue;
                        }

                        break;
                }
            }
        }

        foreach (var auditEntry in trailEntries.Where(_ => !_.HasTemporaryProperties))
        {
            AuditTrails.Add(auditEntry.ToAuditTrail());
        }

        return trailEntries.Where(_ => _.HasTemporaryProperties).ToList();
    }

    private Task OnAfterSaveChangesAsync(List<AuditTrail> trailEntries, in CancellationToken cancellationToken = new())
    {
        if (trailEntries == null || trailEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var entry in trailEntries)
        {
            foreach (var prop in entry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    entry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    entry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditTrails.Add(entry.ToAuditTrail());
        }

        return SaveChangesAsync(cancellationToken);
    }
}