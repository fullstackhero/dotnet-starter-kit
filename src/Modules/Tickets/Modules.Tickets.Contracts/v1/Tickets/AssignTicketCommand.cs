using Mediator;

namespace FSH.Modules.Tickets.Contracts.v1.Tickets;

/// <summary>
/// Assigns a ticket to a user. Pass null `AssigneeUserId` to clear the
/// assignment. Triggers a status transition to InProgress when the
/// ticket is currently Open.
/// </summary>
public sealed record AssignTicketCommand(Guid TicketId, Guid? AssigneeUserId) : ICommand<Guid>;
