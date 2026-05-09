using FSH.Framework.Core.Exceptions;

namespace FSH.Starter.WebApi.Water.Domain.Exceptions;

public sealed class MeterTroubleTicketNotFoundException(Guid id) : NotFoundException($"meter trouble ticket with id {id} not found")
{
}
