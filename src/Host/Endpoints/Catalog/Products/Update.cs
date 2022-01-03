using Ardalis.ApiEndpoints;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Common;
using DN.WebApi.Domain.Constants;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Host.Controllers;
using DN.WebApi.Infrastructure.Identity.Permissions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

[ApiConventionType(typeof(FSHApiConventions))]
public class Update : EndpointBaseAsync
    .WithRequest<IdFromRouteWithBody<UpdateProductRequest>>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Update> _localizer;
    private readonly IFileStorageService _file;

    public Update(IRepositoryAsync repository, IStringLocalizer<Update> localizer, IFileStorageService file)
    {
        _repository = repository;
        _localizer = localizer;
        _file = file;
    }

    [HttpPut("{id:guid}")]
    [MustHavePermission(PermissionConstants.Products.Update)]
    [OpenApiOperation("Update a product.", "")]
    public override async Task<Result<Guid>> HandleAsync([FromRoute] IdFromRouteWithBody<UpdateProductRequest> request, CancellationToken cancellationToken = default)
    {
        var product = await _repository.GetByIdAsync<Product>(request.Id, null);
        if (product == null) throw new EntityNotFoundException(string.Format(_localizer["product.notfound"], request.Id));
        string? productImagePath = null;
        if (request.Body.Image != null) productImagePath = await _file.UploadAsync<Product>(request.Body.Image, FileType.Image);
        if (request.Body.BrandId != default)
        {
            var brand = await _repository.GetByIdAsync<Brand>(request.Body.BrandId, null);
            if (brand == null) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], request.Id));
        }

        var updatedProduct = product.Update(request.Body.Name, request.Body.Description, request.Body.Rate, request.Body.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductUpdatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        await _repository.UpdateAsync(updatedProduct);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(request.Id);
    }
}