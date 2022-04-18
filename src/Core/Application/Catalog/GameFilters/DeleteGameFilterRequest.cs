using FSH.WebApi.Application.Catalog.Products;
using FSH.WebApi.Domain.Common.Events;

namespace FSH.WebApi.Application.Catalog.Filters;

public class DeleteFilterRequest : IRequest<Guid>
{
    public Guid Id { get; set; }

    public DeleteFilterRequest(Guid id) => Id = id;
}

public class DeleteFilterRequestHandler : IRequestHandler<DeleteFilterRequest, Guid>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<Filter> _FilterRepo;
    
    private readonly IStringLocalizer _t;

    public DeleteFilterRequestHandler(IRepositoryWithEvents<Filter> FilterRepo,   IStringLocalizer<DeleteFilterRequestHandler> localizer) =>
        (_FilterRepo,  _t) = (FilterRepo,   localizer);

    public async Task<Guid> Handle(DeleteFilterRequest request, CancellationToken cancellationToken)
    {
        //if (await _productRepo.AnyAsync(new ProductsByBrandSpec(request.Id), cancellationToken))
        //{
        //    throw new ConflictException(_t["Game type cannot be deleted as it's being used."]);
        //}

        var Filter = await _FilterRepo.GetByIdAsync(request.Id, cancellationToken);

        _ = Filter ?? throw new NotFoundException(_t["Game type {0} Not Found."]);
        // Add Domain Events to be raised after the commit
        Filter.DomainEvents.Add(EntityDeletedEvent.WithEntity(Filter));
        await _FilterRepo.DeleteAsync(Filter, cancellationToken);

        return request.Id;
    }
}