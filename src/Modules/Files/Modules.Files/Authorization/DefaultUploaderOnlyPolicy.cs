using System.Security.Claims;
using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Files.Authorization;

/// <summary>
/// Default policy used for the built-in <c>MyFiles</c> and <c>User</c> owner types.
/// - Attach: any authenticated user can attach.
/// - Read: Public files visible to anyone in tenant; Private files only to the uploader.
/// - Delete: only the uploader.
///
/// Tenant scoping is handled by the framework's BaseDbContext (schema-per-tenant), not here.
/// Owning modules with different rules (e.g. Tickets — participants only) register their own
/// <see cref="IFileAccessPolicy"/> implementation that supersedes this one.
/// </summary>
internal sealed class DefaultUploaderOnlyPolicy : IFileAccessPolicy
{
    public DefaultUploaderOnlyPolicy(string ownerType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        OwnerType = ownerType;
    }

    public string OwnerType { get; }

    public Task<bool> CanAttachAsync(Guid? ownerId, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        return Task.FromResult(user.Identity?.IsAuthenticated == true);
    }

    public Task<bool> CanReadAsync(FileAccessContext context, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);
        if (user.Identity?.IsAuthenticated != true) return Task.FromResult(false);

        // Visibility 0 = Public, visible to anyone in the tenant.
        if (context.Visibility == 0) return Task.FromResult(true);
        return Task.FromResult(IsUploader(context, user));
    }

    public Task<bool> CanDeleteAsync(FileAccessContext context, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(user);
        if (user.Identity?.IsAuthenticated != true) return Task.FromResult(false);
        return Task.FromResult(IsUploader(context, user));
    }

    private static bool IsUploader(FileAccessContext context, ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return !string.IsNullOrEmpty(sub) && string.Equals(sub, context.CreatedByUserId, StringComparison.Ordinal);
    }
}
