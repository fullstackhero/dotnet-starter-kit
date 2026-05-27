using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain.Events;

public sealed record MessageCreatedDomainEvent(
    Guid ChannelId,
    Guid MessageId,
    string AuthorUserId,
    Guid? ParentMessageId,
    Guid EventId,
    DateTimeOffset OccurredOnUtc) : DomainEvent(EventId, OccurredOnUtc);
