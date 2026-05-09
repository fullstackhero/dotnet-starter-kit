using System.ComponentModel;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Bills.Create.v1;

public sealed record CreateBillCommand(
    Guid CustomerId,
    Guid? TariffId,
    int BillingMonth,
    int BillingYear,
    [property: DefaultValue(0)] decimal TotalConsumption = 0,
    [property: DefaultValue(0)] decimal FixedCharge = 0,
    [property: DefaultValue(0)] decimal VariableCharge = 0,
    [property: DefaultValue(0)] decimal TotalAmount = 0,
    DateTimeOffset DueDate = default) : IRequest<CreateBillResponse>;
