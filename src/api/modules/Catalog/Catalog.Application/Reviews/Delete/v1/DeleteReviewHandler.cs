using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Delete.v1;
public sealed class DeleteReviewHandler(
    ILogger<DeleteReviewHandler> logger,
    [FromKeyedServices("catalog:reviews")] IRepository<Review> repository)
    : IRequestHandler<DeleteReviewCommand>
{
    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var review = await repository.GetByIdAsync(request.Id, cancellationToken);
        _ = review ?? throw new ReviewNotFoundException(request.Id);
        await repository.DeleteAsync(review, cancellationToken);
        logger.LogInformation("Review with id : {ReviewId} deleted", review.Id);
    }
}
