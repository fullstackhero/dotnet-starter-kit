using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Catalog.Domain.Events;

namespace FSH.Starter.WebApi.Catalog.Domain;
public class Review : AuditableEntity, IAggregateRoot
{
    public string Reviewer { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public int Score { get; private set; }
    public DateTime ReviewDate { get; private set; }
    public Guid AgencyId { get; private set; } // New property for Agency reference

    private Review() { }

    private Review(Guid id, string reviewer, string content, int score, DateTime reviewDate, Guid agencyId)
    {
        Id = id;
        Reviewer = reviewer;
        Content = content;
        Score = score;
        ReviewDate = reviewDate;
        AgencyId = agencyId;

        QueueDomainEvent(new ReviewCreated { Review = this });
    }

    public static Review Create(string reviewer, string content, int score, DateTime reviewDate, Guid agencyId)
    {
        return new Review(Guid.NewGuid(), reviewer, content, score, reviewDate, agencyId);
    }

    public Review Update(string? reviewer, string? content, int? score, DateTime? reviewDate, Guid? agencyId)
    {
        bool isUpdated = false;

        if (!string.IsNullOrWhiteSpace(reviewer) && !string.Equals(Reviewer, reviewer, StringComparison.OrdinalIgnoreCase))
        {
            Reviewer = reviewer;
            isUpdated = true;
        }

        if (!string.IsNullOrWhiteSpace(content) && !string.Equals(Content, content, StringComparison.OrdinalIgnoreCase))
        {
            Content = content;
            isUpdated = true;
        }

        if (score.HasValue && Score != score.Value)
        {
            Score = score.Value;
            isUpdated = true;
        }

        if (reviewDate.HasValue && ReviewDate != reviewDate.Value)
        {
            ReviewDate = reviewDate.Value;
            isUpdated = true;
        }

        if (agencyId.HasValue && AgencyId != agencyId.Value)
        {
            AgencyId = agencyId.Value;
            isUpdated = true;
        }

        if (isUpdated)
        {
            QueueDomainEvent(new ReviewUpdated { Review = this });
        }

        return this;
    }
}
