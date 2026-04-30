using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Tickets.Contracts.v1.Tickets;
using FSH.Modules.Tickets.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Tickets.Features.v1.Tickets.AddTicketComment;

public sealed class AddTicketCommentCommandHandler(
    TicketsDbContext dbContext,
    ICurrentUser currentUser)
    : ICommandHandler<AddTicketCommentCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddTicketCommentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var authorId = currentUser.GetUserId();
        if (authorId == Guid.Empty)
        {
            throw new CustomException(
                "Cannot post a comment without an authenticated author.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Unauthorized);
        }

        var ticket = await dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == command.TicketId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket {command.TicketId} not found.");

        var commentId = ticket.AddComment(authorId, command.Body);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return commentId;
    }
}
