using System.Net;
using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Domain.Events;

namespace FSH.Modules.Tickets.Domain;

/// <summary>
/// Ticket aggregate — encapsulates the lifecycle state machine and owns
/// its comment collection. State transitions are guarded: each public
/// method throws CustomException when called from an illegal state, so
/// the API surface returns clean 409s for invalid transitions.
/// </summary>
public sealed class Ticket : AggregateRoot<Guid>, ISoftDeletable
{
    private readonly List<TicketComment> _comments = [];

    public string Number { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public TicketStatus Status { get; private set; }
    public TicketPriority Priority { get; private set; }
    public Guid ReporterUserId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? ResolutionNote { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public IReadOnlyCollection<TicketComment> Comments => _comments.AsReadOnly();

    /// <summary>
    /// Reverses a soft delete. Idempotent.
    /// </summary>
    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private Ticket() { }

    public static Ticket Create(
        string number,
        string title,
        string? description,
        TicketPriority priority,
        Guid reporterUserId,
        Guid? assignedToUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        // A ticket assigned at creation jumps straight to InProgress —
        // there's no point in flicking through Open for a single tick.
        var initialStatus = assignedToUserId is not null ? TicketStatus.InProgress : TicketStatus.Open;

        var ticket = new Ticket
        {
            Id = Guid.CreateVersion7(),
            Number = number,
            Title = title.Trim(),
            Description = description?.Trim(),
            Priority = priority,
            Status = initialStatus,
            ReporterUserId = reporterUserId,
            AssignedToUserId = assignedToUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        ticket.AddDomainEvent(DomainEvent.Create<TicketCreatedDomainEvent>(
            (id, ts) => new TicketCreatedDomainEvent(
                ticket.Id, ticket.Number, ticket.Title, ticket.Priority,
                ticket.ReporterUserId, ticket.AssignedToUserId, id, ts)));

        return ticket;
    }

    public void Assign(Guid? assigneeUserId)
    {
        ThrowIfClosedOrResolved("assign");

        if (assigneeUserId == AssignedToUserId)
        {
            return;
        }

        var previous = AssignedToUserId;
        AssignedToUserId = assigneeUserId;
        UpdatedAtUtc = DateTime.UtcNow;

        // Picking up a ticket implicitly starts it (Open → InProgress); unassigning an InProgress
        // ticket sends it back to Open since no owner is pushing it forward.
        if (assigneeUserId is not null && Status == TicketStatus.Open)
        {
            TransitionStatus(TicketStatus.InProgress);
        }
        else if (assigneeUserId is null && Status == TicketStatus.InProgress)
        {
            TransitionStatus(TicketStatus.Open);
        }

        AddDomainEvent(DomainEvent.Create<TicketAssignedDomainEvent>(
            (id, ts) => new TicketAssignedDomainEvent(Id, previous, assigneeUserId, id, ts)));
    }

    public void Resolve(string? resolutionNote)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new CustomException(
                "A closed ticket cannot be resolved — reopen it first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
        if (Status == TicketStatus.Resolved)
        {
            return;
        }

        ResolutionNote = string.IsNullOrWhiteSpace(resolutionNote) ? null : resolutionNote.Trim();
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        TransitionStatus(TicketStatus.Resolved);
    }

    /// <summary>
    /// Finalizes a resolved ticket (Resolved → Closed). Idempotent when already Closed;
    /// rejects any other source state with a 409 so the documented state machine holds.
    /// </summary>
    public void Close()
    {
        if (Status == TicketStatus.Closed)
        {
            return;
        }
        if (Status != TicketStatus.Resolved)
        {
            throw new CustomException(
                $"Only a resolved ticket can be closed — current status is {Status}. Resolve it first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        ClosedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
        TransitionStatus(TicketStatus.Closed);
    }

    /// <summary>
    /// Edits the mutable details of an open/in-progress/resolved ticket. A closed ticket is
    /// frozen — it must be reopened first.
    /// </summary>
    public void UpdateDetails(string title, string? description, TicketPriority priority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (Status == TicketStatus.Closed)
        {
            throw new CustomException(
                "A closed ticket cannot be edited — reopen it first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Priority = priority;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reopen()
    {
        if (Status is TicketStatus.Open or TicketStatus.InProgress)
        {
            return;
        }

        // Clear the close timestamp so a fresh resolution gets its own audit window.
        ClosedAtUtc = null;
        ResolvedAtUtc = null;
        ResolutionNote = null;
        UpdatedAtUtc = DateTime.UtcNow;
        // Reopened tickets fall back to whatever assignment state they had:
        // if still assigned, InProgress; if not, Open.
        TransitionStatus(AssignedToUserId is null ? TicketStatus.Open : TicketStatus.InProgress);
    }

    public Guid AddComment(Guid authorUserId, string body)
    {
        if (Status == TicketStatus.Closed)
        {
            throw new CustomException(
                "A closed ticket cannot accept new comments — reopen it first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        var comment = TicketComment.Create(Id, authorUserId, body);
        _comments.Add(comment);
        UpdatedAtUtc = DateTime.UtcNow;

        AddDomainEvent(DomainEvent.Create<TicketCommentAddedDomainEvent>(
            (id, ts) => new TicketCommentAddedDomainEvent(Id, comment.Id, authorUserId, id, ts)));

        return comment.Id;
    }

    private void TransitionStatus(TicketStatus next)
    {
        if (next == Status)
        {
            return;
        }

        var previous = Status;
        Status = next;
        AddDomainEvent(DomainEvent.Create<TicketStatusChangedDomainEvent>(
            (id, ts) => new TicketStatusChangedDomainEvent(Id, previous, next, id, ts)));
    }

    private void ThrowIfClosedOrResolved(string action)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Resolved)
        {
            throw new CustomException(
                $"Cannot {action} a ticket in status {Status} — reopen it first.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
    }
}
