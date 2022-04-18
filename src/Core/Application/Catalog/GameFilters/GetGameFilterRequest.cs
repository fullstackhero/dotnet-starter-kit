namespace FSH.WebApi.Application.Catalog.Filters;

public class GetFilterRequest : IRequest<FilterDto>
{
    public Guid Id { get; set; }

    public GetFilterRequest(Guid id) => Id = id;
}

public class FilterByIdSpec : Specification<Filter, FilterDto>, ISingleResultSpecification
{
    public FilterByIdSpec(Guid id) =>
        Query.Where(p => p.Id == id);
}

public class GetFilterRequestHandler : IRequestHandler<GetFilterRequest, FilterDto>
{
    private readonly IRepository<Filter> _repository;
    private readonly IStringLocalizer _t;

    public GetFilterRequestHandler(IRepository<Filter> repository, IStringLocalizer<GetFilterRequestHandler> localizer) => (_repository, _t) = (repository, localizer);

    public async Task<FilterDto> Handle(GetFilterRequest request, CancellationToken cancellationToken) =>
        await _repository.GetBySpecAsync(
            (ISpecification<Filter, FilterDto>)new FilterByIdSpec(request.Id), cancellationToken)
        ?? throw new NotFoundException(_t["Filter {0} Not Found.", request.Id]);
}