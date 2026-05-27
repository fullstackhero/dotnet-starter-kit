using FSH.Framework.Core.Domain;

namespace FSH.Modules.Webhooks.Domain;

public sealed class WebhookSubscription : BaseEntity<Guid>
{
    public string Url { get; private set; } = default!;
    public string EventsCsv { get; private set; } = default!;
    public string? SecretHash { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private WebhookSubscription() { }

    public static WebhookSubscription Create(string url, string[] events, string? secretHash)
    {
        ArgumentNullException.ThrowIfNull(url);
        ArgumentNullException.ThrowIfNull(events);

        return new WebhookSubscription
        {
            Id = Guid.CreateVersion7(),
            Url = url,
            EventsCsv = string.Join(',', events),
            SecretHash = secretHash,
            IsActive = true,
            CreatedAtUtc = TimeProvider.System.GetUtcNow().UtcDateTime
        };
    }

    public string[] GetEvents() => EventsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public bool MatchesEvent(string eventType) =>
        GetEvents().Contains(eventType, StringComparer.OrdinalIgnoreCase) ||
        GetEvents().Contains("*");

    public void Deactivate() => IsActive = false;
}
