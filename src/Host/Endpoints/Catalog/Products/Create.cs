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
public class Create : EndpointBaseAsync
    .WithRequest<CreateProductRequest>
    .WithResult<Result<Guid>>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<Create> _localizer;
    private readonly IFileStorageService _file;

    public Create(IRepositoryAsync repository, IStringLocalizer<Create> localizer, IFileStorageService file)
    {
        _repository = repository;
        _localizer = localizer;
        _file = file;
    }

    [HttpPost("")]
    [MustHavePermission(PermissionConstants.Products.Register)]
    [OpenApiOperation("Create a new product.", "")]
    public override async Task<Result<Guid>> HandleAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        bool productExists = await _repository.ExistsAsync<Product>(a => a.Name == request.Name);
        if (productExists) throw new EntityAlreadyExistsException(string.Format(_localizer["product.alreadyexists"], request.Name));
        bool brandExists = await _repository.ExistsAsync<Brand>(a => a.Id == request.BrandId);
        if (!brandExists) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], request.BrandId));
        string productImagePath = await _file.UploadAsync<Product>(request.Image, FileType.Image);
        var product = new Product(request.Name, request.Description, request.Rate, request.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductCreatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        var productId = await _repository.CreateAsync(product);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(productId);
    }
}