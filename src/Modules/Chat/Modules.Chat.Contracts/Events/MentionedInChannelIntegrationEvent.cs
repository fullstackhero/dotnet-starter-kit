using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.Chat.Contracts.Events;

/// <summary>
/// One event per resolved <c>@user</c> mention inside a sent <see cref="MessageId"/>. Consumed by
/// the Notifications module to materialize a bell-icon row + push a SignalR event to the
/// mentioned user's connections. Owns no PII beyond the body preview (truncated upstream).
/// </summary>
public sealed record MentionedInChannelIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid ChannelId,
    string? ChannelName,
    Guid MessageId,
    string AuthorUserId,
    string MentionedUserId,
    string BodyPreview) : IIntegrationEvent;
