using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record AddTicketCommentCommand(Guid TicketId, string Body) : ICommand<Guid>;
