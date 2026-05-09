using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Bills.Get.v1;

public sealed record BillResponse(
    Guid? Id,
    Guid CustomerId,
    Guid? TariffId,
    int BillingMonth,
    int BillingYear,
    decimal TotalConsumption,
    decimal TotalAmount,
    decimal FixedCharge,
    decimal VariableCharge,
    DateTimeOffset DueDate,
    DateTimeOffset? PaidDate,
    BillStatus Status);
