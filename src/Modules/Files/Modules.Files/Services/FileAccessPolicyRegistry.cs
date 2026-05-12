using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Files.Services;

/// <summary>
/// Resolves an <see cref="IFileAccessPolicy"/> for a given OwnerType. Owning modules register their
/// policies via <c>services.AddFileAccessPolicy&lt;TPolicy&gt;()</c> (see <see cref="FileAccessPolicyExtensions"/>).
/// Lookup is case-insensitive. Unknown OwnerType returns null — handlers should treat that as
/// "policy missing → forbidden" (closed-by-default).
/// </summary>
internal sealed class FileAccessPolicyRegistry
{
    private readonly Dictionary<string, IFileAccessPolicy> _policies;

    public FileAccessPolicyRegistry(IEnumerable<IFileAccessPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        _policies = new Dictionary<string, IFileAccessPolicy>(StringComparer.OrdinalIgnoreCase);
        foreach (var policy in policies)
        {
            // Last-write-wins on duplicate OwnerType registrations; this is convenient for tests
            // that swap a real policy with a substitute. Production registrations should not collide.
            _policies[policy.OwnerType] = policy;
        }
    }

    public IFileAccessPolicy? Resolve(string ownerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        return _policies.TryGetValue(ownerType, out var policy) ? policy : null;
    }
}
