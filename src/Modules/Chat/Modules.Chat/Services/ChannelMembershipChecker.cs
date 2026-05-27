using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Services;

/// <summary>
/// Chat module's adapter that satisfies the realtime hub's membership probe. Scoped — the
/// hub instantiates one per call.
/// </summary>
public sealed class ChannelMembershipChecker(ChatDbContext db) : IChannelMembershipChecker
{
    public async ValueTask<bool> IsMemberAsync(Guid channelId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        return await db.Channels
            .AnyAsync(c => c.Id == channelId && c.Members.Any(m => m.UserId == userId), cancellationToken)
            .ConfigureAwait(false);
    }
}
