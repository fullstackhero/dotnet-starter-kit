using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Features.v1.Internal;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages.SendMessage;

public sealed class SendMessageCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub)
    : ICommandHandler<SendMessageCommand, MessageDto>
{
    public async ValueTask<MessageDto> Handle(SendMessageCommand cmd, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cmd);
        var userId = currentUser.GetUserId();
        if (userId == Guid.Empty) throw new UnauthorizedException("no current user");
        var currentUserId = userId.ToString();

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == cmd.ChannelId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Channel not found.");

        channel.RequireMember(currentUserId);

        Message? parent = null;
        if (cmd.ParentMessageId is { } parentId)
        {
            parent = await db.Messages.FirstOrDefaultAsync(m => m.Id == parentId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("Parent message not found.");
            if (parent.ChannelId != channel.Id)
            {
                throw new CustomException("Parent message belongs to a different channel.", (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
            }
            if (parent.ParentMessageId.HasValue)
            {
                // 1-level deep only per spec.
                throw new CustomException("Cannot reply to a reply — threads are single-level only.", (IEnumerable<string>?)null, HttpStatusCode.BadRequest);
            }
        }

        var message = Message.Create(channel.Id, currentUserId, cmd.Body, parent?.Id);
        foreach (var att in cmd.Attachments ?? Array.Empty<SendMessageAttachmentInput>())
        {
            message.AddAttachment(att.FileAssetId, att.Url, att.ContentType, att.FileName, att.SizeBytes);
        }

        db.Messages.Add(message);

        if (parent is not null)
        {
            parent.IncrementReplyCount();
        }
        channel.TouchLastMessage(DateTime.UtcNow);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = message.ToDto();
        await hub.Clients.Group($"channel:{channel.Id}")
            .SendAsync("ChatMessageCreated", dto, cancellationToken)
            .ConfigureAwait(false);
        return dto;
    }
}
