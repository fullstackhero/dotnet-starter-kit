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

public class Update : EndpointBaseAsync
    .WithRequest<IdFromRouteWithBody<UpdateBrandRequest>>
    .WithActionResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Update> _localizer;

    public Update(IRepositoryAsync repository, IStringLocalizer<Update> localizer) =>
        (_repository, _localizer) = (repository, localizer);

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Brands.Update)]
    public override async Task<ActionResult<Result<Guid>>> HandleAsync([FromRoute] IdFromRouteWithBody<UpdateBrandRequest> request, CancellationToken cancellationToken = default)
    {
        var brand = await _repository.GetByIdAsync<Brand>(request.Id, cancellationToken: cancellationToken);
        if (brand is null)
        {
            return this.NotFoundError(string.Format(_localizer["brand.notfound"], request.Id));
        }

        if (await _repository.ExistsAsync<Brand>(a => a.Id != request.Id && a.Name == request.Body.Name))
        {
            return this.ConflictError(string.Format(_localizer["brand.alreadyexists"], request.Body.Name));
        }

        var updatedBrand = brand.Update(request.Body.Name, request.Body.Description);
        updatedBrand.DomainEvents.Add(new StatsChangedEvent());
        await _repository.UpdateAsync(updatedBrand, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}