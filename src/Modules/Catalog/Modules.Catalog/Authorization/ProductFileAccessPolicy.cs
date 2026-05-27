using FSH.Modules.Files.Contracts;

namespace FSH.Modules.Catalog.Authorization;

/// <summary>
/// IFileAccessPolicy for product images (OwnerType=Product).
///
/// - Attach: any authenticated user. The product update endpoint's permission check
///   (Catalog.Products.Update) is the durable gate — orphaned uploads that never land on a product
///   are reaped by the Files module's orphan-purge job after the upload deadline.
/// - Read: open. Product images are surfaced publicly via Visibility=Public + the durable URL
///   minted by GetFileMetadata. Private products would need a different policy.
/// - Delete: uploader-only. A future refinement could grant Catalog.Products.Update holders
///   delete rights too once we wire IUserPermissionService here.
/// </summary>
public sealed class ProductFileAccessPolicy : IFileAccessPolicy
{
    public string OwnerType => "Product";

    public Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(!string.IsNullOrEmpty(currentUserId));

    public Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }
}
