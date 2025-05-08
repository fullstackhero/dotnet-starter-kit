using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.EventHandlers;

public sealed class ReviewUpdatedEventHandler : INotificationHandler<ReviewUpdated>
{
    private readonly ILogger<ReviewUpdatedEventHandler> _logger;

    public ReviewUpdatedEventHandler(ILogger<ReviewUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ReviewUpdated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Review updated: {ReviewId}", notification.Review.Id);
        return Task.CompletedTask;
    }
}
