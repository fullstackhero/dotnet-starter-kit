using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Get.v1;

public sealed class GetReviewHandler(
    IReadRepository<Review> repository)
    : IRequestHandler<GetReviewRequest, ReviewResponse>
{
    public async Task<ReviewResponse> Handle(GetReviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var review = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = review ?? throw new ReviewNotFoundException(request.Id);

        return new ReviewResponse(
            review.Id,
            review.Reviewer,
            review.Content,
            review.Score,
            review.ReviewDate);
    }
}
