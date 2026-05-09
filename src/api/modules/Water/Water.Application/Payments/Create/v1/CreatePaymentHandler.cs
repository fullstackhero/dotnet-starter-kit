using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Payments.Create.v1;

public sealed class CreatePaymentHandler(
    ILogger<CreatePaymentHandler> logger,
    [FromKeyedServices("water:payments")] IRepository<Payment> repository)
    : IRequestHandler<CreatePaymentCommand, CreatePaymentResponse>
{
    public async Task<CreatePaymentResponse> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var payment = Payment.Create(request.BillId, request.AmountPaid, request.PaymentDate, request.PaymentMethod, request.TransactionReference);
        await repository.AddAsync(payment, cancellationToken);
        logger.LogInformation("payment created {PaymentId}", payment.Id);
        return new CreatePaymentResponse(payment.Id);
    }
}
