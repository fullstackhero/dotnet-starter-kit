using System.Collections.ObjectModel;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>
/// All currently-pinned messages in a channel, ordered by PinnedAtUtc desc.
/// </summary>
public sealed record GetPinnedMessagesQuery(Guid ChannelId)
    : IQuery<ReadOnlyCollection<MessageDto>>;
