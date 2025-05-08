using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Create.v1;
public sealed class CreateReviewHandler(
    ILogger<CreateReviewHandler> logger,
    [FromKeyedServices("catalog:reviews")] IRepository<Review> repository)
    : IRequestHandler<CreateReviewCommand, CreateReviewResponse>
{
    public async Task<CreateReviewResponse> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var review = Review.Create(request.Reviewer, request.Content, request.Score, request.ReviewDate, request.AgencyId);
        await repository.AddAsync(review, cancellationToken);
        logger.LogInformation("review created {ReviewId}", review.Id);
        return new CreateReviewResponse(review.Id);
    }
}
