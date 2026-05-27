namespace FSH.Modules.Webhooks.Contracts.Dtos;

public sealed class WebhookDeliveryDto
{
    public Guid Id { get; init; }
    public Guid SubscriptionId { get; init; }
    public string EventType { get; init; } = default!;
    public int HttpStatusCode { get; init; }
    public bool Success { get; init; }
    public int AttemptCount { get; init; }
    public DateTime AttemptedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
}
