using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Endpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Application.Catalog.Products;

public class Delete : EndpointBaseAsync
    .WithRequest<IdFromRoute>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;

    public Delete(IRepositoryAsync repository) => _repository = repository;

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = PermissionConstants.Products.Remove)]
    [OpenApiOperation("Delete a product.", "")]
    public override async Task<Result<Guid>> HandleAsync([FromRoute] IdFromRoute request, CancellationToken cancellationToken = default)
    {
        var productToDelete = await _repository.RemoveByIdAsync<Product>(request.Id);
        productToDelete.DomainEvents.Add(new ProductDeletedEvent(productToDelete));
        productToDelete.DomainEvents.Add(new StatsChangedEvent());
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(request.Id);
    }
}