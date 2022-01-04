using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class Delete : EndpointBaseAsync
    .WithRequest<DeleteProductRequest>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;

    public Delete(IRepositoryAsync repository) => _repository = repository;

    [HttpDelete("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Remove)]
    [OpenApiOperation("Delete a product.", "")]
    public override async Task<Result<Guid>> HandleAsync([FromRoute] DeleteProductRequest request, CancellationToken cancellationToken = default)
    {
        var productToDelete = await _repository.RemoveByIdAsync<Product>(request.Id, cancellationToken);

        productToDelete.DomainEvents.Add(new ProductDeletedEvent(productToDelete));
        productToDelete.DomainEvents.Add(new StatsChangedEvent());

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}