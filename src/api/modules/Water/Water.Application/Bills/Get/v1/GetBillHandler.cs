using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.Bills.Get.v1;

public sealed class GetBillHandler(
    [FromKeyedServices("water:bills")] IReadRepository<Bill> repository,
    ICacheService cache)
    : IRequestHandler<GetBillRequest, BillResponse>
{
    public async Task<BillResponse> Handle(GetBillRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"bill:{request.Id}",
            async () =>
            {
                var bill = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (bill == null) throw new BillNotFoundException(request.Id);
                return new BillResponse(bill.Id, bill.CustomerId, bill.TariffId, bill.BillingMonth, bill.BillingYear, bill.TotalConsumption, bill.TotalAmount, bill.FixedCharge, bill.VariableCharge, bill.DueDate, bill.PaidDate, bill.Status);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
