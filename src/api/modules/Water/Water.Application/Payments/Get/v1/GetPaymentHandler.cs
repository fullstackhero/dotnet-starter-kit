using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Payments.Get.v1;

public sealed class GetPaymentHandler(
    [FromKeyedServices("water:payments")] IReadRepository<Payment> repository,
    ICacheService cache)
    : IRequestHandler<GetPaymentRequest, PaymentResponse>
{
    public async Task<PaymentResponse> Handle(GetPaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"payment:{request.Id}",
            async () =>
            {
                var payment = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (payment == null) throw new PaymentNotFoundException(request.Id);
                return new PaymentResponse(payment.Id, payment.BillId, payment.AmountPaid, payment.PaymentDate, payment.PaymentMethod, payment.TransactionReference, payment.Status);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
