using FSH.Modules.Chat.Data;
using FSH.Modules.Files.Contracts;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Authorization;

/// <summary>
/// IFileAccessPolicy for chat-channel attachments (OwnerType=ChatChannel).
///
/// - Attach: caller must be a current member of the target channel (ownerId).
/// - Read: caller must be a current member of the channel that owns the file.
/// - Delete: uploader-only. Channel admins can already moderate the message itself
///   (Messages.DeleteAny), which cascades to attachments through the message FK.
///
/// Tenant scoping is enforced upstream by Finbuckle; this policy only needs to gate by
/// channel membership.
/// </summary>
public sealed class ChatChannelFileAccessPolicy(ChatDbContext db) : IFileAccessPolicy
{
    /// <summary>Owner-type token clients pass on RequestUploadUrl (and that we read here).</summary>
    public const string OwnerTypeName = "ChatChannel";

    public string OwnerType => OwnerTypeName;

    public async Task<bool> CanAttachAsync(Guid? ownerId, string currentUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(currentUserId)) return false;
        if (ownerId is not { } channelId) return false;
        return await db.Channels.AsNoTracking()
            .AnyAsync(c => c.Id == channelId && c.Members.Any(m => m.UserId == currentUserId), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> CanReadAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrEmpty(currentUserId)) return false;
        if (context.OwnerId is not { } channelId) return false;
        return await db.Channels.AsNoTracking()
            .AnyAsync(c => c.Id == channelId && c.Members.Any(m => m.UserId == currentUserId), cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<bool> CanDeleteAsync(FileAccessContext context, string currentUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(
            !string.IsNullOrEmpty(currentUserId)
            && string.Equals(currentUserId, context.CreatedByUserId, StringComparison.Ordinal));
    }
}
