using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Channels.FindOrCreateDm;

public sealed class FindOrCreateDmCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser)
    : ICommandHandler<FindOrCreateDmCommand, Guid>
{
    public async ValueTask<Guid> Handle(FindOrCreateDmCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var otherIds = cmd.UserIds.Distinct(StringComparer.Ordinal).ToList();
        if (otherIds.Any(id => string.Equals(id, currentUserId, StringComparison.Ordinal)))
        {
            throw new CustomException("Cannot DM yourself.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.BadRequest);
        }

        if (otherIds.Count == 1)
        {
            // Two-person DM — deterministic lookup via DirectKey.
            var (lo, hi) = string.CompareOrdinal(currentUserId, otherIds[0]) < 0
                ? (currentUserId, otherIds[0])
                : (otherIds[0], currentUserId);
            var directKey = $"{lo}:{hi}";

            var existing = await db.Channels
                .FirstOrDefaultAsync(c => c.Type == ChannelType.DirectMessage && c.DirectKey == directKey, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null) return existing.Id;

            var dm = ChatChannel.CreateDirect(currentUserId, otherIds[0]);
            db.Channels.Add(dm);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return dm.Id;
        }

        // Group DM (3+ total members including the caller). Always created fresh.
        var allUserIds = new List<string>(otherIds.Count + 1) { currentUserId };
        allUserIds.AddRange(otherIds);
        var group = ChatChannel.CreateGroupDm(allUserIds, currentUserId);
        db.Channels.Add(group);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return group.Id;
    }
}
