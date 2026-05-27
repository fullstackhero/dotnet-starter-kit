namespace FSH.Modules.Chat.Services;

/// <summary>
/// Resolves a batch of <c>@username</c> mentions to user ids. Returns a map keyed by the
/// username token (without the leading <c>@</c>). Missing usernames are simply absent from the
/// dictionary — the caller treats those mentions as unresolved.
/// </summary>
public interface IMentionResolver
{
    Task<IReadOnlyDictionary<string, string>> ResolveUserIdsAsync(
        IReadOnlyCollection<string> usernames,
        CancellationToken cancellationToken = default);
}
