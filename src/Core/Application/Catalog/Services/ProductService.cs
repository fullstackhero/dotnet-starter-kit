using DN.WebApi.Application.Catalog.Interfaces;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Specifications;
using DN.WebApi.Application.Storage;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Common;
using DN.WebApi.Domain.Common.Contracts;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Shared.DTOs.Catalog;
using Mapster;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Services;

public class ProductService : IProductService
{
    private readonly IStringLocalizer<ProductService> _localizer;
    private readonly IFileStorageService _file;
    private readonly IRepositoryAsync _repository;

    public ProductService(IRepositoryAsync repository, IStringLocalizer<ProductService> localizer, IFileStorageService file)
    {
        _repository = repository;
        _localizer = localizer;
        _file = file;
    }

    public async Task<Result<Guid>> CreateProductAsync(CreateProductRequest request)
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

    public async Task<Result<Guid>> UpdateProductAsync(UpdateProductRequest request, Guid id)
    {
        var product = await _repository.GetByIdAsync<Product>(id, null);
        if (product == null) throw new EntityNotFoundException(string.Format(_localizer["product.notfound"], id));
        string productImagePath = null;
        if (request.Image != null) productImagePath = await _file.UploadAsync<Product>(request.Image, FileType.Image);
        if (request.BrandId != default)
        {
            var brand = await _repository.GetByIdAsync<Brand>(request.BrandId, null);
            if (brand == null) throw new EntityNotFoundException(string.Format(_localizer["brand.notfound"], id));
        }

        var updatedProduct = product.Update(request.Name, request.Description, request.Rate, request.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductUpdatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        await _repository.UpdateAsync(updatedProduct);
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(id);
    }

    public async Task<Result<Guid>> DeleteProductAsync(Guid id)
    {
        var productToDelete = await _repository.RemoveByIdAsync<Product>(id);
        productToDelete.DomainEvents.Add(new ProductDeletedEvent(productToDelete));
        productToDelete.DomainEvents.Add(new StatsChangedEvent());
        await _repository.SaveChangesAsync();
        return await Result<Guid>.SuccessAsync(id);
    }

    public async Task<Result<ProductDetailsDto>> GetProductDetailsAsync(Guid id)
    {
        var spec = new BaseSpecification<Product>();
        spec.Includes.Add(a => a.Brand);
        var product = await _repository.GetByIdAsync<Product, ProductDetailsDto>(id, spec);
        return await Result<ProductDetailsDto>.SuccessAsync(product);
    }

    public async Task<Result<ProductDto>> GetByIdUsingDapperAsync(Guid id)
    {
        var product = await _repository.QueryFirstOrDefaultAsync<Product>($"SELECT * FROM public.\"Products\" WHERE \"Id\"  = '{id}' AND \"Tenant\"='@tenant'");
        var mappedProduct = product.Adapt<ProductDto>();
        return await Result<ProductDto>.SuccessAsync(mappedProduct);
    }

    public async Task<PaginatedResult<ProductDto>> SearchAsync(ProductListFilter filter)
    {
        var filters = new Filters<Product>();
        filters.Add(filter.BrandId.HasValue, x => x.BrandId.Equals(filter.BrandId.Value));
        filters.Add(filter.MinimumRate.HasValue, x => x.Rate >= filter.MinimumRate.Value);
        filters.Add(filter.MaximumRate.HasValue, x => x.Rate <= filter.MaximumRate.Value);

        return await _repository.GetSearchResultsAsync<Product, ProductDto>(filter.PageNumber, filter.PageSize, filter.OrderBy, filters, filter.AdvancedSearch, filter.Keyword);
    }
}