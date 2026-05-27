using FSH.Modules.Identity.Contracts.Services;

namespace FSH.Modules.Chat.Services;

/// <summary>
/// Default mention resolver. Pulls the full user list once and filters in memory — fine for
/// the scale this starter kit targets and avoids adding a by-username lookup to
/// <see cref="IUserService"/> (Identity contract stays unchanged).
///
/// For larger tenants, swap to a targeted lookup via a new IUserService method or a dedicated
/// Identity endpoint.
/// </summary>
public sealed class MentionResolver(IUserService users) : IMentionResolver
{
    public async Task<IReadOnlyDictionary<string, string>> ResolveUserIdsAsync(
        IReadOnlyCollection<string> usernames,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usernames);
        if (usernames.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        // Normalize to a set so repeated mentions in one message hit the lookup only once.
        var lookup = new HashSet<string>(usernames, StringComparer.OrdinalIgnoreCase);

        var all = await users.GetListAsync(cancellationToken).ConfigureAwait(false);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in all)
        {
            if (string.IsNullOrEmpty(u.UserName) || string.IsNullOrEmpty(u.Id)) continue;
            if (lookup.Contains(u.UserName) && u.IsActive)
            {
                result[u.UserName] = u.Id;
            }
        }
        return result;
    }
}
