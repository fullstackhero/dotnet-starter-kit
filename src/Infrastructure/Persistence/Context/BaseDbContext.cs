using System.Data;
using Finbuckle.MultiTenant;
using FL_CRMS_ERP_WEBAPI.Application.Common.Events;
using FL_CRMS_ERP_WEBAPI.Application.Common.Interfaces;
using FL_CRMS_ERP_WEBAPI.Domain.Common.Contracts;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Auditing;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Options;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Context;

public abstract class BaseDbContext : MultiTenantIdentityDbContext<ApplicationUser, ApplicationRole, string, IdentityUserClaim<string>, IdentityUserRole<string>, IdentityUserLogin<string>, ApplicationRoleClaim, IdentityUserToken<string>>
{
    protected readonly ICurrentUser _currentUser;
    private readonly ISerializerService _serializer;
    private readonly DatabaseSettings _dbSettings;
    private readonly IEventPublisher _events;

    protected BaseDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, IEventPublisher events)
        : base(currentTenant, options)
    {
        _currentUser = currentUser;
        _serializer = serializer;
        _dbSettings = dbSettings.Value;
        _events = events;
    }

    // Used by Dapper
    public IDbConnection Connection => Database.GetDbConnection();

    public DbSet<Trail> AuditTrails => Set<Trail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // QueryFilters need to be applied before base.OnModelCreating
        modelBuilder.AppendGlobalQueryFilter<ISoftDelete>(s => s.DeletedOn == null);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // TODO: We want this only for development probably... maybe better make it configurable in logger.json config?
        optionsBuilder.EnableSensitiveDataLogging();

        // If you want to see the sql queries that efcore executes:

        // Uncomment the next line to see them in the output window of visual studio
        // optionsBuilder.LogTo(m => System.Diagnostics.Debug.WriteLine(m), Microsoft.Extensions.Logging.LogLevel.Information);

        // Or uncomment the next line if you want to see them in the console
        // optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);

        if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString))
        {
            optionsBuilder.UseDatabase(_dbSettings.DBProvider, TenantInfo.ConnectionString);
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        var auditEntries = HandleAuditingBeforeSaveChanges(_currentUser.GetUserId());

        int result = await base.SaveChangesAsync(cancellationToken);

        await HandleAuditingAfterSaveChangesAsync(auditEntries, cancellationToken);

        await SendDomainEventsAsync();

        return result;
    }

    private List<AuditTrail> HandleAuditingBeforeSaveChanges(Guid userId)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.LastModifiedBy = userId;
                    break;

                case EntityState.Modified:
                    entry.Entity.LastModifiedOn = DateTime.UtcNow;
                    entry.Entity.LastModifiedBy = userId;
                    break;

                case EntityState.Deleted:
                    if (entry.Entity is ISoftDelete softDelete)
                    {
                        softDelete.DeletedBy = userId;
                        softDelete.DeletedOn = DateTime.UtcNow;
                        entry.State = EntityState.Modified;
                    }

                    break;
            }
        }

        ChangeTracker.DetectChanges();

        var trailEntries = new List<AuditTrail>();
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .ToList())
        {
            var trailEntry = new AuditTrail(entry, _serializer)
            {
                TableName = entry.Entity.GetType().Name,
                UserId = userId
            };

            //TimeLine for all modules started from here

            var _addedEntities = ChangeTracker.Entries()
             .Select(e => e.Entity)
             .ToList();

            if (trailEntry.TableName == "TaskModel")
            {
               foreach (var entrys in _addedEntities)
                {
                    if (entrys is TaskModel myModel)
                    {
                        trailEntry.LeadId = myModel.WhoId;
                        trailEntry.Subject = myModel.Subject;
                        trailEntry.RelatedTo = myModel.RelatedTo;
                        // Do something with the name
                    }
                }
            }
            else if(trailEntry.TableName == "CallsModel")
            {

                foreach (var entrys in _addedEntities)
                {
                    if (entrys is CallsModel myModel)
                    {
                        trailEntry.LeadId = myModel.WhoId;
                        trailEntry.Subject = myModel.Subject;
                        trailEntry.RelatedTo = myModel.RelatedTo;
                        // Do something with the name
                    }
                }
            }
            else if(trailEntry.TableName == "MeetingModel")
            {
                foreach (var entrys in _addedEntities)
                {
                    if (entrys is MeetingModel myModel)
                    {
                        trailEntry.MeetingLeadId = myModel.Participants;
                        trailEntry.Subject = myModel.MeetingTitle;
                        trailEntry.RelatedTo = myModel.RelatedTo;
                        // Do something with the name
                    }
                }
            }
            else if(trailEntry.TableName == "NotesModel")
            {
                foreach (var entrys in _addedEntities)
                {
                    if (entrys is NotesModel myModel)
                    {
                        trailEntry.LeadId = myModel.ParentId;
                        trailEntry.Subject = myModel.NoteTitle;
                        trailEntry.RelatedTo = myModel.RelatedTo;
                        // Do something with the name
                    }
                }
            }
            else if(trailEntry.TableName == "AccountDetailsModel")
            {
                foreach (var entrys in _addedEntities)
                {
                    if (entrys is AccountDetailsModel myModel)
                    {
                        trailEntry.LeadId = myModel.ConvertedLeadId;
                        trailEntry.Subject = myModel.AccountName;
                        // Do something with the name
                    }
                }
            }
            else if (trailEntry.TableName == "LeadDetailsModel")
            {
                foreach (var entrys in _addedEntities)
                {
                    if (entrys is LeadDetailsModel myModel)
                    {
                        trailEntry.LeadId = myModel.Id;
                        trailEntry.Subject = myModel.CompanyName;
                        // Do something with the name
                    }
                }
            }
            else if (trailEntry.TableName == "ContactDetailsModel")
            {
                foreach (var entrys in _addedEntities)
                {
                    if (entrys is ContactDetailsModel myModel)
                    {
                        trailEntry.LeadId = myModel.LeadId;
                        trailEntry.Subject = myModel.FirstName;
                        // Do something with the name
                    }
                }
            }
            //else if (trailEntry.TableName == "LeadDetailsModel")
            //{
            //    foreach (var entrys in _addedEntities)
            //    {
            //        if (entrys is LeadDetailsModel myModel)
            //        {
            //            trailEntry.LeadId = myModel.Id;
            //            trailEntry.Subject = myModel.CompanyName;
            //            // Do something with the name
            //        }
            //    }
            //}
            //


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

        foreach (var auditEntry in trailEntries.Where(e => !e.HasTemporaryProperties))
        {
            AuditTrails.Add(auditEntry.ToAuditTrail());
        }

        return trailEntries.Where(e => e.HasTemporaryProperties).ToList();
    }

    private Task HandleAuditingAfterSaveChangesAsync(List<AuditTrail> trailEntries, CancellationToken cancellationToken = new())
    {
        if (trailEntries == null || trailEntries.Count == 0)
        {
            return Task.CompletedTask;
        }

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

    private async Task SendDomainEventsAsync()
    {
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
                await _events.PublishAsync(domainEvent);
            }
        }
    }
}