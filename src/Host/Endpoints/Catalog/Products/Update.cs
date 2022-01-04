using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Common;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class Update : EndpointBaseAsync
    .WithRequest<IdFromRouteWithBody<UpdateProductRequest>>
    .WithActionResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Update> _localizer;
    private readonly IFileStorageService _file;

    public Update(IRepositoryAsync repository, IStringLocalizer<Update> localizer, IFileStorageService file) =>
        (_repository, _localizer, _file) = (repository, localizer, file);

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Update)]
    [OpenApiOperation("Update a product.", "")]
    public override async Task<ActionResult<Result<Guid>>> HandleAsync([FromRoute] IdFromRouteWithBody<UpdateProductRequest> request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync<Product>(request.Id, cancellationToken: cancellationToken);
        if (product is null)
        {
            return this.NotFoundError(string.Format(_localizer["product.notfound"], request.Id));
        }

        if (await _repository.ExistsAsync<Product>(p => p.Id != request.Id && p.Name == request.Body.Name))
        {
            return this.ConflictError(string.Format(_localizer["product.alreadyexists"], request.Body.Name));
        }

        string? productImagePath = request.Body.Image is not null
            ? await _file.UploadAsync<Product>(request.Body.Image, FileType.Image)
            : null;

        var updatedProduct = product.Update(request.Body.Name, request.Body.Description, request.Body.Rate, request.Body.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductUpdatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        await _repository.UpdateAsync(updatedProduct, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}