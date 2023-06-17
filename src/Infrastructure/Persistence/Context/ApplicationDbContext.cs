using Finbuckle.MultiTenant;
using FL_CRMS_ERP_WEBAPI.Application.Common.Events;
using FL_CRMS_ERP_WEBAPI.Application.Common.Interfaces;
using FL_CRMS_ERP_WEBAPI.Domain.Catalog;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.Identity;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(ITenantInfo currentTenant, DbContextOptions options, ICurrentUser currentUser, ISerializerService serializer, IOptions<DatabaseSettings> dbSettings, IEventPublisher events)
        : base(currentTenant, options, currentUser, serializer, dbSettings, events)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<LeadDetailsModel> LeadDetailsInfo => Set<LeadDetailsModel>();
    public DbSet<NotesModel> NotesDetailsInfo => Set<NotesModel>();
    public DbSet<TaskModel> TaskDetailsInfo => Set<TaskModel>();
    public DbSet<MeetingModel> MeetingDetailsInfo => Set<MeetingModel>();
    public DbSet<CallsModel> CallDetailsInfo => Set<CallsModel>();

    public DbSet<AccountDetailsModel> AccountDetailsInfo => Set<AccountDetailsModel>();

    public DbSet<ContactDetailsModel> ContactDetailsInfo => Set<ContactDetailsModel>();
    public DbSet<QuotationDetailsModel> QuotationDetailsInfo => Set<QuotationDetailsModel>();
    public DbSet<InvoiceDetailsModel> InvoiceDetailsInfo => Set<InvoiceDetailsModel>();
    public DbSet<PersonalDetailsModel> PersonalDetailsInfo => Set<PersonalDetailsModel>();
    public DbSet<CustomerDetailsModel> CustomerDetailsInfo => Set<CustomerDetailsModel>();
    public DbSet<ProductDetailsModel> ProductDetailsInfo => Set<ProductDetailsModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaNames.Catalog);
        //modelBuilder.HasDefaultSchema(SchemaNames.LeadData);

    }
}