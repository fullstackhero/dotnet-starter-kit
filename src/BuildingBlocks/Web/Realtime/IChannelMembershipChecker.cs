namespace FSH.Framework.Web.Realtime;

/// <summary>
/// Realtime hub asks this whether a user may broadcast or receive on a given channel. Implemented
/// by the Chat module's runtime (queries the channel membership table). Lives in BuildingBlocks/Web
/// so the hub stays decoupled from any concrete module.
/// </summary>
public interface IChannelMembershipChecker
{
    ValueTask<bool> IsMemberAsync(Guid channelId, string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lookup of the channel ids a user belongs to. The hub uses this on connect to pre-join the
/// user's connection to all of their channel groups, so subsequent broadcasts reach them without
/// a per-message membership check.
/// </summary>
public interface IUserChannelLookup
{
    ValueTask<IReadOnlyList<Guid>> ListMyChannelIdsAsync(string userId, CancellationToken cancellationToken = default);
}
