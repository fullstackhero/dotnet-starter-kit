using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class MeterNotFoundException(Guid id) : NotFoundException($"meter with id {id} not found")
{
}
