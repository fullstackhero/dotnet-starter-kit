using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class BillNotFoundException(Guid id) : NotFoundException($"bill with id {id} not found")
{
}
