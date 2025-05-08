using FSH.Framework.Core.Paging;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Search.v1;

public class SearchReviewsCommand : PaginationFilter, IRequest<PagedList<ReviewResponse>>
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}
