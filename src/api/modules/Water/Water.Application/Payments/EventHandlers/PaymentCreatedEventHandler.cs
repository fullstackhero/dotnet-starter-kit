using FSH.Starter.WebApi.Water.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Payments.EventHandlers;

public class PaymentCreatedEventHandler(ILogger<PaymentCreatedEventHandler> logger) : INotificationHandler<PaymentCreated>
{
    public async Task Handle(PaymentCreated notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("handling payment created domain event..");
        await Task.FromResult(notification);
        logger.LogInformation("finished handling payment created domain event..");
    }
}
