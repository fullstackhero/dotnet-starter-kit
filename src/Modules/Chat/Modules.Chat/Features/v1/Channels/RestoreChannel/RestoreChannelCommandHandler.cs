using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Localization;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.RestoreChannel;

public sealed class RestoreChannelCommandHandler(ChatDbContext db)
    : ICommandHandler<RestoreChannelCommand, Unit>
{
    public async ValueTask<Unit> Handle(RestoreChannelCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);

        // IgnoreQueryFilters bypasses the SoftDelete filter so we can find an archived channel.
        var channel = await db.Channels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.")
            {
                MessageKey = "Chat.ChannelNotFound",
                ResourceSource = typeof(ChatResources),
            };

        if (!channel.IsDeleted) return Unit.Value; // idempotent
        channel.Restore();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
