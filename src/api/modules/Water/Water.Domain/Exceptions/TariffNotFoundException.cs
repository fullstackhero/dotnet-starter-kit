using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class TariffNotFoundException(Guid id) : NotFoundException($"tariff with id {id} not found")
{
}
