using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.WebApi.Water.Application.Bills.Create.v1;

public sealed class CreateBillHandler(
    ILogger<CreateBillHandler> logger,
    [FromKeyedServices("water:bills")] IRepository<Bill> repository)
    : IRequestHandler<CreateBillCommand, CreateBillResponse>
{
    public async Task<CreateBillResponse> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var bill = Bill.Create(request.CustomerId, request.TariffId, request.BillingMonth, request.BillingYear, request.TotalConsumption, request.FixedCharge, request.VariableCharge, request.TotalAmount, request.DueDate);
        await repository.AddAsync(bill, cancellationToken);
        logger.LogInformation("bill created {BillId}", bill.Id);
        return new CreateBillResponse(bill.Id);
    }
}
