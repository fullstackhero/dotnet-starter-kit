using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class Create : EndpointBaseAsync
    .WithRequest<CreateBrandRequest>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;

    public Create(IRepositoryAsync repository) => _repository = repository;

    [HttpPost("")]
    [MustHavePermission(PermissionConstants.Brands.Register)]
    public override async Task<Result<Guid>> HandleAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = new Brand(request.Name, request.Description);
        brand.DomainEvents.Add(new StatsChangedEvent());
        var brandId = await _repository.CreateAsync(brand, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(brandId);
    }
}