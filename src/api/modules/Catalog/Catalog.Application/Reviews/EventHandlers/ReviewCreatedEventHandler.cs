using FSH.Starter.WebApi.Catalog.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.EventHandlers;

public sealed class ReviewCreatedEventHandler : INotificationHandler<ReviewCreated>
{
    private readonly ILogger<ReviewCreatedEventHandler> _logger;

    public ReviewCreatedEventHandler(ILogger<ReviewCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(ReviewCreated notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Review created: {ReviewId}", notification.Review.Id);
        return Task.CompletedTask;
    }
}
