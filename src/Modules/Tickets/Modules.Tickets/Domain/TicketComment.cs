using FSH.Framework.Core.Domain;

namespace FSH.Modules.Tickets.Domain;

/// <summary>
/// A comment posted on a ticket. Created via Ticket.AddComment so that
/// the parent aggregate stays the consistency boundary — comments are
/// never persisted independently.
/// </summary>
public sealed class TicketComment : BaseEntity<Guid>, ISoftDeletable
{
    public Guid TicketId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private TicketComment() { }

    internal static TicketComment Create(Guid ticketId, Guid authorUserId, string body)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new TicketComment
        {
            Id = Guid.CreateVersion7(),
            TicketId = ticketId,
            AuthorUserId = authorUserId,
            Body = body.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
