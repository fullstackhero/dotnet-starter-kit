using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Payments.Get.v1;

public sealed record PaymentResponse(
    Guid? Id,
    Guid BillId,
    decimal AmountPaid,
    DateTimeOffset PaymentDate,
    PaymentMethod PaymentMethod,
    string? TransactionReference,
    PaymentStatus Status);
