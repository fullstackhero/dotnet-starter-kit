using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Search.v1;

public sealed class SearchReviewsHandler(
    IReadRepository<Review> repository)
    : IRequestHandler<SearchReviewsCommand, PagedList<ReviewResponse>>
{
    public async Task<PagedList<ReviewResponse>> Handle(SearchReviewsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var spec = new SearchReviewSpecs(request);

        var items = await repository.ListAsync(spec, cancellationToken).ConfigureAwait(false);
        var totalCount = await repository.CountAsync(spec, cancellationToken).ConfigureAwait(false);

        return new PagedList<ReviewResponse>(items, request!.PageNumber, request!.PageSize, totalCount);
    }
}
