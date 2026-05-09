using System.ComponentModel;
using FSH.Starter.WebApi.Water.Domain;
using MediatR;

namespace FSH.Starter.WebApi.Water.Application.Payments.Create.v1;

public sealed record CreatePaymentCommand(
    Guid BillId,
    [property: DefaultValue(0)] decimal AmountPaid = 0,
    DateTimeOffset PaymentDate = default,
    PaymentMethod PaymentMethod = PaymentMethod.Cash,
    string? TransactionReference = null) : IRequest<CreatePaymentResponse>;
