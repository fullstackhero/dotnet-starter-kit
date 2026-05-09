using FSH.Framework.Core.Caching;
using FSH.Framework.Core.Persistence;
using FSH.Starter.WebApi.Water.Domain;
using FSH.Starter.WebApi.Water.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Get.v1;

public sealed class GetMeterTroubleTicketHandler(
    [FromKeyedServices("water:trouble-tickets")] IReadRepository<MeterTroubleTicket> repository,
    ICacheService cache)
    : IRequestHandler<GetMeterTroubleTicketRequest, MeterTroubleTicketResponse>
{
    public async Task<MeterTroubleTicketResponse> Handle(GetMeterTroubleTicketRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var item = await cache.GetOrSetAsync(
            $"trouble-ticket:{request.Id}",
            async () =>
            {
                var ticket = await repository.GetByIdAsync(request.Id, cancellationToken);
                if (ticket == null) throw new MeterTroubleTicketNotFoundException(request.Id);
                return new MeterTroubleTicketResponse(ticket.Id, ticket.MeterId, ticket.ReportedDate, ticket.IssueDescription, ticket.Status, ticket.ResolvedDate, ticket.ResolutionNotes);
            },
            cancellationToken: cancellationToken);
        return item!;
    }
}
