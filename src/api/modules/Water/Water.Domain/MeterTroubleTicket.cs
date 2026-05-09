using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class MeterTroubleTicket : AuditableEntity, IAggregateRoot
{
    public Guid MeterId { get; private set; }
    public virtual Meter Meter { get; private set; } = default!;
    public DateTimeOffset ReportedDate { get; private set; }
    public string IssueDescription { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; }
    public DateTimeOffset? ResolvedDate { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private MeterTroubleTicket() { }

    private MeterTroubleTicket(Guid id, Guid meterId, DateTimeOffset reportedDate, string issueDescription)
    {
        Id = id;
        MeterId = meterId;
        ReportedDate = reportedDate;
        IssueDescription = issueDescription;
        Status = TicketStatus.Open;

        QueueDomainEvent(new MeterTroubleTicketCreated { MeterTroubleTicket = this });
    }

    public static MeterTroubleTicket Create(Guid meterId, DateTimeOffset reportedDate, string issueDescription)
    {
        return new MeterTroubleTicket(Guid.NewGuid(), meterId, reportedDate, issueDescription);
    }

    public MeterTroubleTicket Update(string? issueDescription, TicketStatus? status, string? resolutionNotes, DateTimeOffset? resolvedDate)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(issueDescription) && !string.Equals(IssueDescription, issueDescription, StringComparison.OrdinalIgnoreCase))
        {
            IssueDescription = issueDescription;
            isUpdated = true;
        }

        if (status.HasValue && Status != status.Value)
        {
            Status = status.Value;
            isUpdated = true;

            if (status.Value == TicketStatus.Resolved || status.Value == TicketStatus.Closed)
            {
                ResolvedDate = resolvedDate ?? DateTimeOffset.UtcNow;
            }
        }

        if (!string.Equals(ResolutionNotes, resolutionNotes, StringComparison.OrdinalIgnoreCase))
        {
            ResolutionNotes = resolutionNotes;
            isUpdated = true;
        }

        if (isUpdated && (Status == TicketStatus.Resolved || Status == TicketStatus.Closed))
        {
            QueueDomainEvent(new MeterTroubleTicketResolved { MeterTroubleTicket = this });
        }

        return this;
    }

    public MeterTroubleTicket Resolve(string? resolutionNotes)
    {
        Status = TicketStatus.Resolved;
        ResolvedDate = DateTimeOffset.UtcNow;
        ResolutionNotes = resolutionNotes;

        QueueDomainEvent(new MeterTroubleTicketResolved { MeterTroubleTicket = this });

        return this;
    }
}
