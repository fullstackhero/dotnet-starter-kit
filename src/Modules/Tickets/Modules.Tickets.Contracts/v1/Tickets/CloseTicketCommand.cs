using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record CloseTicketCommand(Guid TicketId) : ICommand<Guid>;
