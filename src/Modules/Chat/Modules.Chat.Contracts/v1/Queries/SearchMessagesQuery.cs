using System.Collections.ObjectModel;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Queries;

/// <summary>
/// Full-text message search scoped to channels the caller is a member of. Results ranked by
/// <c>ts_rank</c>. <paramref name="ChannelId"/> narrows the search to a single channel; when null
/// it searches across every channel the caller belongs to.
/// </summary>
public sealed record SearchMessagesQuery(string Q, Guid? ChannelId = null, int Page = 1, int PageSize = 50)
    : IQuery<ReadOnlyCollection<MessageDto>>;
