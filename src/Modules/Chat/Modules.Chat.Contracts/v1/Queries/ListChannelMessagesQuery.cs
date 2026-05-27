using System.Collections.ObjectModel;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>
/// Cursor-paged top-level messages in a channel (no replies). Reverse-chronological by
/// <c>Message.Id</c> (Guid v7 is monotonic so Id desc = time desc).
/// </summary>
public sealed record ListChannelMessagesQuery(Guid ChannelId, Guid? Before, int PageSize = 50)
    : IQuery<ReadOnlyCollection<MessageDto>>;
