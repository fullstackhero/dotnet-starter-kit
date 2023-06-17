using FL_CRMS_ERP_WEBAPI.Infrastructure.Multitenancy;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Persistence.Initialization;

internal interface IDatabaseInitializer
{
    Task InitializeDatabasesAsync(CancellationToken cancellationToken);
    Task InitializeApplicationDbForTenantAsync(FSHTenantInfo tenant, CancellationToken cancellationToken);
}