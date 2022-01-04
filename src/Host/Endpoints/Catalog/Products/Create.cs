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
using NSwag.Annotations;

namespace DN.WebApi.Host.Endpoints.Catalog.Products;

public class Create : EndpointBaseAsync
    .WithRequest<CreateProductRequest>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IFileStorageService _file;

    public Create(IRepositoryAsync repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    [HttpPost("")]
    [MustHavePermission(PermissionConstants.Products.Register)]
    [OpenApiOperation("Create a new product.", "")]
    public override async Task<Result<Guid>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        string productImagePath = await _file.UploadAsync<Product>(request.Image, FileType.Image, cancellationToken);

        var product = new Product(request.Name, request.Description, request.Rate, request.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductCreatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        var productId = await _repository.CreateAsync(product, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(productId);
    }
}