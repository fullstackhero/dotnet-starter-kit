using DN.WebApi.Application.Common.Persistence;
using DN.WebApi.Application.Identity.Users;
using DN.WebApi.Domain.Multitenancy;
using MediatR;

namespace DN.WebApi.Application.Multitenancy;

public class CreateTenantRequest : IRequest<Guid>
{
    public string? Name { get; set; }
    public string Key { get; set; } = default!;
    public string? AdminEmail { get; set; }
    public string? ConnectionString { get; set; }
}

public class CreateTenantRequestHandler : IRequestHandler<CreateTenantRequest, Guid>
{
    private readonly ITenantRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly ITenantDatabaseService _tenantDbService;

    public CreateTenantRequestHandler(ITenantRepository repository, ICurrentUser currentUser, ITenantDatabaseService tenantDbService) =>
        (_repository, _currentUser, _tenantDbService) = (repository, currentUser, tenantDbService);

    public async Task<Guid> Handle(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.ConnectionString))
        {
            request.ConnectionString = _tenantDbService.DefaultConnectionString;
        }

        var tenant = new Tenant(request.Name, request.Key, request.AdminEmail, request.ConnectionString)
        {
            CreatedBy = _currentUser.GetUserId()
        };

        await _repository.AddAsync(tenant, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        try
        {
            _tenantDbService.InitializeDatabase(tenant);
        }
        catch
        {
            await _repository.DeleteAsync(tenant, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw;
        }

        return tenant.Id;
    }
}