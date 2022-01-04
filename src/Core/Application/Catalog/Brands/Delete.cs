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

public class Delete : EndpointBaseAsync
    .WithRequest<IdFromRoute>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Delete> _localizer;

    public Delete(IRepositoryAsync repository, IStringLocalizer<Delete> localizer)
    {
        _repository = repository;
        _localizer = localizer;
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionConstants.Brands.Remove)]
    public override async Task<Result<Guid>> HandleAsync([FromRoute] IdFromRoute request, CancellationToken cancellationToken = default)
    {
        bool isBrandUsed = await _repository.ExistsAsync<Product>(a => a.BrandId == request.Id);
        if (isBrandUsed) throw new EntityCannotBeDeleted(_localizer["brand.cannotbedeleted"]);
        var brandToDelete = await _repository.RemoveByIdAsync<Brand>(request.Id);
        brandToDelete.DomainEvents.Add(new StatsChangedEvent());
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(request.Id);
    }
}