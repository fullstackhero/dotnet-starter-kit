using System.Collections.ObjectModel;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>
/// Cursor-paged thread replies (a.k.a. messages whose <c>ParentMessageId</c> equals
/// <paramref name="ParentMessageId"/>). Reverse-chronological by Id (Guid v7 monotonic).
/// </summary>
public sealed record ListMessageRepliesQuery(Guid ParentMessageId, Guid? Before, int PageSize = 50)
    : IQuery<ReadOnlyCollection<MessageDto>>;
