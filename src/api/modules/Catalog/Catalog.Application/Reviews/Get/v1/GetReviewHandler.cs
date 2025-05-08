using Microsoft.Extensions.DependencyInjection;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Core.Caching;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;

public sealed class GetReviewHandler(
   [FromKeyedServices("catalog:reviews")] IReadRepository<Review> repository,
    ICacheService cache)
    : IRequestHandler<GetReviewRequest, ReviewResponse>
{
    public async Task<ReviewResponse> Handle(GetReviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"Review:{request.Id}",
            async () =>
            {
                var ReviewItem = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (ReviewItem == null) throw new ReviewNotFoundException(request.Id);
                return new ReviewResponse(ReviewItem.Id, ReviewItem.Reviewer, ReviewItem.Content, ReviewItem.Score, ReviewItem.ReviewDate);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
