using System.Globalization;
using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using FSH.Modules.Tickets.Data;
using FSH.Modules.Tickets.Domain;
using Mediator;
using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Features.v1.Tickets.CreateTicket;

public sealed class CreateTicketCommandHandler(
    TicketsDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<CreateTicketCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTicketCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var reporterId = currentUser.GetUserId();
        if (reporterId == Guid.Empty)
        {
            throw new CustomException(
                "Cannot create a ticket without an authenticated reporter.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Unauthorized);
        }

        // Sequential, tenant-scoped ticket numbers (TK-1, TK-2, …).
        //
        // We count ALL tenant rows including soft-deleted ones (via
        // IgnoreQueryFilters) — otherwise a deleted TK-3 would let us
        // re-issue TK-3 to a new ticket, which collides with the live
        // unique index AND is a confusing audit footprint. Two writers
        // racing here will both compute the same next-number; the
        // filtered unique index on `Number` rejects the second insert
        // and the API surface returns 409 — caller can retry.
        //
        // For higher write contention, the right upgrade is a dedicated
        // counter table updated via `UPDATE … RETURNING` (or a Postgres
        // sequence per tenant). The demo doesn't need it.
        long count = await dbContext.Tickets
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
        string number = $"TK-{(count + 1).ToString(CultureInfo.InvariantCulture)}";

        var ticket = Ticket.Create(
            number: number,
            title: command.Title,
            description: command.Description,
            priority: command.Priority,
            reporterUserId: reporterId,
            assignedToUserId: command.AssignedToUserId);

        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ticket.Id;
    }
}
