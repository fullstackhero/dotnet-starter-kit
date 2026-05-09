using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class MeterReadingNotFoundException(Guid id) : NotFoundException($"meter reading with id {id} not found")
{
}
