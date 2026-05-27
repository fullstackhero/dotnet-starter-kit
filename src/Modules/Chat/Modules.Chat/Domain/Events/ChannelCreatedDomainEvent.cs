using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain.Events;

public sealed record ChannelCreatedDomainEvent(
    Guid ChannelId,
    ChannelType Type,
    string? Name,
    string CreatedByUserId,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
