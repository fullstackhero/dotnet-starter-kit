namespace FSH.Modules.Files.Contracts;

/// <summary>
/// Per-OwnerType authorization for FileAssets. Each owning module (Catalog, Tickets, ...) registers
/// its own implementation via <c>services.AddFileAccessPolicy&lt;TPolicy&gt;()</c>. The Files module
/// ships a uploader-only default for the built-in <c>MyFiles</c> and <c>User</c> owner types.
/// Tenant scoping is enforced by the framework's BaseDbContext (schema-per-tenant) and is NOT
/// delegated to policies. Policies receive a primitive <c>currentUserId</c> rather than a
/// <c>ClaimsPrincipal</c> so the contract stays free of ASP.NET Core types — owning modules that
/// need richer authz can inject their own dependencies.
/// </summary>
public interface IFileAccessPolicy
{
    /// <summary>The OwnerType this policy handles. Must be unique across registered policies.</summary>
    string OwnerType { get; }

    Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken);

    Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken);

    Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken);
}

/// <summary>
/// Minimal projection of a FileAsset passed to <see cref="IFileAccessPolicy"/> methods so owning
/// modules can make access decisions without depending on the Files module runtime.
/// </summary>
/// <param name="FileAssetId">FileAsset identity.</param>
/// <param name="OwnerType">OwnerType value.</param>
/// <param name="OwnerId">OwnerId value, or null when the file isn't bound to an owner (e.g. MyFiles).</param>
/// <param name="CreatedByUserId">User who uploaded the file.</param>
/// <param name="Visibility">0 = Public, 1 = Private (matches the runtime enum int).</param>
public sealed record FileAccessContext(
    Guid FileAssetId,
    string OwnerType,
    Guid? OwnerId,
    string CreatedByUserId,
    int Visibility);
