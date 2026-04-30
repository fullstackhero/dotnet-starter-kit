using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

public sealed record ResolveTicketCommand(Guid TicketId, string? ResolutionNote = null) : ICommand<Guid>;
