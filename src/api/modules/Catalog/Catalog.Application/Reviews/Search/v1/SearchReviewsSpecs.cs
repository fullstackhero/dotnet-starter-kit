using Ardalis.Specification;
using FSH.Framework.Core.Paging;
using FSH.Framework.Core.Specifications;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;
using FSH.Starter.WebApi.Catalog.Domain;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Search.v1;

public class SearchReviewSpecs : EntitiesByPaginationFilterSpec<Review, ReviewResponse>
{
    public SearchReviewSpecs(SearchReviewsCommand command)
        : base(command) =>
        Query
            .OrderBy(c => c.Reviewer, !command.HasOrderBy())
            .Where(r => r.Content.Contains(command.Keyword), !string.IsNullOrEmpty(command.Keyword));
}
