using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Update.v1;

public sealed class UpdateReviewHandler(
    ILogger<UpdateReviewHandler> logger,
    IRepository<Review> repository)
    : IRequestHandler<UpdateReviewCommand, UpdateReviewResponse>
{
    public async Task<UpdateReviewResponse> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var Review = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = Review ?? throw new ReviewNotFoundException(request.Id);
        var updatedReview = Review.Update(request.Reviewer, request.Content, request.Score, request.ReviewDate, request.AgencyId);
        await repository.UpdateAsync(updatedReview, cancellationToken);
        logger.LogInformation("Review with id : {ReviewId} updated.", Review.Id);
        return new UpdateReviewResponse(Review.Id);
    }
}
