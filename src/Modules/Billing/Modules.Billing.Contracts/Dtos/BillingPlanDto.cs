using FSH.Framework.Shared.Quota;

namespace FSH.Modules.Billing.Contracts.Dtos;

public sealed record BillingPlanDto(
    Guid Id,
    string Key,
    string Name,
    string Currency,
    decimal MonthlyBasePrice,
    IReadOnlyDictionary<QuotaResource, decimal> OverageRates,
    bool IsActive);
