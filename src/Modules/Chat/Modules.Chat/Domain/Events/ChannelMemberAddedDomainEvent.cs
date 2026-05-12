using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain.Events;

public sealed record ChannelMemberAddedDomainEvent(
    Guid ChannelId,
    string AddedUserId,
    string AddedByUserId,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
