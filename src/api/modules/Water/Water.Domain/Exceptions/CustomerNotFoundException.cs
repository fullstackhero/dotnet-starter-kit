using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class CustomerNotFoundException(Guid id) : NotFoundException($"customer with id {id} not found")
{
}
