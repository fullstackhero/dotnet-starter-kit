namespace FSH.WebApi.Application.Dogs;
public class SearchDogsRequest : PaginationFilter, IRequest<PaginationResponse<DogDto>>
{
}

public class DogsBySearchReqeustSpec : EntitiesByPaginationFilterSpec<Dog, DogDto>
{
    public DogsBySearchReqeustSpec(SearchDogsRequest request)
        : base(request) =>
        Query.OrderBy(c => c.Name, !request.HasOrderBy());
}

public class SearchDogsRequestHandler : IRequestHandler<SearchDogsRequest, PaginationResponse<DogDto>>
{
    private readonly IReadRepository<Dog> _repository;

    public SearchDogsRequestHandler(IReadRepository<Dog> repository) => _repository = repository;

    public async Task<PaginationResponse<DogDto>> Handle(SearchDogsRequest request, CancellationToken cancellationToken)
    {
        var spec = new DogsBySearchReqeustSpec(request);
        return await _repository.PaginatedListAsync(spec, request.PageNumber, request.PageSize, cancellationToken);
    }
}
