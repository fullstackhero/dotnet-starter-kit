using DN.WebApi.Application.Common.Exceptions;
using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.FileStorage;
using DN.WebApi.Domain.Catalog.Products;
using DN.WebApi.Domain.Common;
using DN.WebApi.Shared.DTOs.FileStorage;
using MediatR;
using Microsoft.Extensions.Localization;

namespace DN.WebApi.Application.Catalog.Products;

public class UpdateProductRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Rate { get; set; }
    public Guid BrandId { get; set; }
    public FileUploadRequest? Image { get; set; }
}

public class UpdateProductRequestHandler : IRequestHandler<UpdateProductRequest, Guid>
{
    private readonly IRepositoryAsync _repository;
    private readonly IStringLocalizer<UpdateProductRequestHandler> _localizer;
    private readonly IFileStorageService _file;

    public UpdateProductRequestHandler(IRepositoryAsync repository, IStringLocalizer<UpdateProductRequestHandler> localizer, IFileStorageService file) =>
        (_repository, _localizer, _file) = (repository, localizer, file);

    public async Task<Guid> Handle(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync<Product>(request.Id, cancellationToken: cancellationToken);
        if (product is null)
        {
            throw new EntityNotFoundException(string.Format(_localizer["product.notfound"], request.Id));
        }

        string? productImagePath = request.Image is not null
            ? await _file.UploadAsync<Product>(request.Image, FileType.Image)
            : null;

        var updatedProduct = product.Update(request.Name, request.Description, request.Rate, request.BrandId, productImagePath);

        // Add Domain Events to be raised after the commit
        product.DomainEvents.Add(new ProductUpdatedEvent(product));

        await _repository.UpdateAsync(updatedProduct, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return request.Id;
    }
}