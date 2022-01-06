using DN.WebApi.Application.Common.Interfaces;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Domain.Catalog;
using DN.WebApi.Domain.Catalog.Events;
using DN.WebApi.Domain.Dashboard;
using MediatR;

namespace DN.WebApi.Application.Catalog.Products;

public class DeleteProductRequest : IRequest<Result<Guid>>
{
    public Guid Id { get; set; }

    public DeleteProductRequest(Guid id) => Id = id;
}

public class DeleteProductRequestHandler : IRequestHandler<DeleteProductRequest, Result<Guid>>
{
    private readonly IRepositoryAsync _repository;

    public DeleteProductRequestHandler(IRepositoryAsync repository) => _repository = repository;

    public async Task<Result<Guid>> Handle(DeleteProductRequest request, CancellationToken cancellationToken)
    {
        var productToDelete = await _repository.RemoveByIdAsync<Product>(request.Id, cancellationToken);

        productToDelete.DomainEvents.Add(new ProductDeletedEvent(productToDelete));
        productToDelete.DomainEvents.Add(new StatsChangedEvent());

        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(request.Id);
    }
}