namespace FSH.Modules.Tickets.Contracts.Dtos;

/// <summary>
/// Lifecycle states a ticket transitions through.
///
/// Allowed transitions:
///   Open        → InProgress (Assign or Start)
///   Open        → Resolved   (resolve without intermediate progress)
///   InProgress  → Resolved
///   Resolved    → Closed     (final)
///   Resolved    → Open       (reopen)
///   Closed      → Open       (reopen)
/// </summary>
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Resolved = 2,
    Closed = 3,
}
