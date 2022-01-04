using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Host.Endpoints.Catalog.Brands;

public class Delete : EndpointBaseAsync
    .WithRequest<DeleteBrandRequest>
    .WithActionResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Delete> _localizer;

    public Delete(IRepositoryAsync repository, IStringLocalizer<Delete> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    [HttpDelete("{id:guid}")]
    [MustHavePermission(PermissionConstants.Brands.Remove)]
    public override async Task<ActionResult<Result<Guid>>> HandleAsync([FromRoute] DeleteBrandRequest request, CancellationToken cancellationToken = default)
    {
        if (await _repository.ExistsAsync<Product>(a => a.BrandId == request.Id))
        {
            return this.ConflictError(_localizer["brand.cannotbedeleted"]);
        }

        var brandToDelete = await _repository.RemoveByIdAsync<Brand>(request.Id, cancellationToken);
        brandToDelete.DomainEvents.Add(new StatsChangedEvent());

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}