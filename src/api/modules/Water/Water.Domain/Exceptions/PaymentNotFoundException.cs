using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class PaymentNotFoundException(Guid id) : NotFoundException($"payment with id {id} not found")
{
}
