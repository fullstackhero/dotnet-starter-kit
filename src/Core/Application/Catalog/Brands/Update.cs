using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Endpoints;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Brands;

public class Update : EndpointBaseAsync
    .WithRequest<IdFromRouteWithBody<UpdateBrandRequest>>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Update> _localizer;

    public Update(IRepositoryAsync repository, IStringLocalizer<Update> localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = PermissionConstants.Brands.Update)]
    public override async Task<Result<Guid>> HandleAsync([FromRoute] IdFromRouteWithBody<UpdateBrandRequest> request, CancellationToken cancellationToken = default)
    {
        var brand = await _repository.GetByIdAsync<Brand>(request.Id);
        if (brand == null) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], request.Id));
        var updatedBrand = brand.Update(request.Body.Name, request.Body.Description);
        updatedBrand.DomainEvents.Add(new StatsChangedEvent());
        await _repository.UpdateAsync(updatedBrand);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(request.Id);
    }
}