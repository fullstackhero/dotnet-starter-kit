using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Common;
using DN.WebApi.Domain.Dashboard;
using DN.WebApi.Shared.DTOs.FileStorage;
using MediatR;

namespace DN.WebApi.Application.Catalog.Products;

public class CreateProductRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public Guid BrandId { get; set; }
    public FileUploadRequest? Image { get; set; }
}

public class CreateProductRequestHandler : IRequestHandler<CreateProductRequest, Guid>
{
    private readonly IRepositoryAsync _repository;
    private readonly IFileStorageService _file;

    public CreateProductRequestHandler(IRepositoryAsync repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<Guid> Handle(CreateProductRequest request, CancellationToken cancellationToken)
    {
        string productImagePath = await _file.UploadAsync<Product>(request.Image, FileType.Image, cancellationToken);

        var product = new Product(request.Name, request.Description, request.Rate, request.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductCreatedEvent(product));
        product.DomainEvents.Add(new StatsChangedEvent());

        await _repository.CreateAsync(product, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}