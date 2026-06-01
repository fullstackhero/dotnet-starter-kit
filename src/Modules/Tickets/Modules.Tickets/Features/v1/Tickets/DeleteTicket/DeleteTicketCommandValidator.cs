using FluentValidation;
using FSH.Modules.Tickets.Contracts.v1.Tickets;

namespace FSH.Modules.Tickets.Features.v1.Tickets.DeleteTicket;

public sealed class DeleteTicketCommandValidator : AbstractValidator<DeleteTicketCommand>
{
    public DeleteTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}
