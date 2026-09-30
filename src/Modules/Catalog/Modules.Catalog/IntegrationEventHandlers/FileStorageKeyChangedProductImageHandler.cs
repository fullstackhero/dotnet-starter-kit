using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Files.Contracts.Events;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.IntegrationEventHandlers;

/// <summary>
/// A <see cref="ProductImage"/> persists the public URL of its FileAsset at attach time. When Files
/// moves that object to a new key (a visibility change, or the #1410 move of legacy public files under
/// <c>public/</c>), the stored URL is rewritten to follow it — the old object is deleted right after
/// the move, so a stale URL would break the product page.
/// </summary>
public sealed class FileStorageKeyChangedProductImageHandler(CatalogDbContext db)
    : IIntegrationEventHandler<FileStorageKeyChangedIntegrationEvent>
{
    public async Task HandleAsync(FileStorageKeyChangedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var images = await db.Set<ProductImage>()
            .Where(i => i.FileAssetId == @event.FileAssetId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var changed = false;
        foreach (var image in images)
        {
            // Idempotent: once rewritten, the URL no longer ends with the old key and RewriteUrl returns null.
            if (@event.RewriteUrl(image.Url) is { } rewritten)
            {
                image.ReplaceUrl(rewritten);
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
