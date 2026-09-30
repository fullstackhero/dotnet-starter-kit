using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Files.Contracts.Events;
using FSH.Modules.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Identity.Events;

/// <summary>
/// Avatars are uploaded through the Files module (OwnerType <c>User</c>, OwnerId = the user) and the
/// resulting public URL is stored on <c>FshUser.ImageUrl</c>. When Files moves that object to a new key
/// (a visibility change, or the #1410 move of legacy public files under <c>public/</c>), the stored URL
/// is rewritten to follow it — the old object is deleted right after the move.
/// </summary>
public sealed class FileStorageKeyChangedAvatarHandler(IdentityDbContext db)
    : IIntegrationEventHandler<FileStorageKeyChangedIntegrationEvent>
{
    private const string UserOwnerType = "User";

    public async Task HandleAsync(FileStorageKeyChangedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!string.Equals(@event.OwnerType, UserOwnerType, StringComparison.OrdinalIgnoreCase)
            || @event.OwnerId is not { } ownerId)
        {
            return;
        }

        var userId = ownerId.ToString();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct).ConfigureAwait(false);

        // Idempotent: once rewritten, the URL no longer ends with the old key and ResolveUrl returns null.
        var rewritten = @event.ResolveUrl(user?.ImageUrl?.ToString());
        if (user is null || rewritten is null)
        {
            return;
        }

        user.ImageUrl = new Uri(rewritten, UriKind.RelativeOrAbsolute);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
