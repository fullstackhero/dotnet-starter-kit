using System.Diagnostics;
using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Web.Realtime;
using FSH.Modules.Chat.Contracts.Events;
using FSH.Modules.Chat.Contracts.v1.Commands;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Features.v1.Internal;
using FSH.Modules.Chat.Services;
using Mediator;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Chat.Features.v1.Messages.SendMessage;

public sealed class SendMessageCommandHandler(
    ChatDbContext db,
    ICurrentUser currentUser,
    IHubContext<AppHub> hub,
    IMentionResolver mentionResolver,
    IEventBus eventBus)
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

        // Parse @username tokens and resolve to user ids. Self-mentions and unresolved tokens are
        // dropped silently — the body text still shows the @ as written. Only mentions that resolve
        // to a real *other* user attach as MessageMention rows + trigger an integration event.
        // Body may be null/empty for attachment-only sends.
        var rawMatches = MentionParser.Parse(cmd.Body ?? string.Empty);
        var distinctNames = rawMatches.Select(m => m.Username)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resolved = distinctNames.Length == 0
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : (Dictionary<string, string>)await mentionResolver
                .ResolveUserIdsAsync(distinctNames, cancellationToken)
                .ConfigureAwait(false);

        var parsedMentions = new List<Message.ParsedMention>();
        var notifyUserIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var match in rawMatches)
        {
            if (!resolved.TryGetValue(match.Username, out var mentionedUserId)) continue;
            if (string.Equals(mentionedUserId, currentUserId, StringComparison.Ordinal)) continue;
            parsedMentions.Add(new Message.ParsedMention(mentionedUserId, match.StartIndex, match.Length));
            notifyUserIds.Add(mentionedUserId);
        }

        var message = Message.Create(channel.Id, currentUserId, cmd.Body, parent?.Id, parsedMentions);
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

        // One integration event per distinct mentioned user. Notifications module subscribes.
        if (notifyUserIds.Count > 0)
        {
            var correlationId = Activity.Current?.TraceId.ToString() ?? Guid.NewGuid().ToString();
            var tenantId = currentUser.GetTenant();
            var preview = MakePreview(message.Body ?? string.Empty);
            foreach (var mentionedUserId in notifyUserIds)
            {
                await eventBus.PublishAsync(
                    new MentionedInChannelIntegrationEvent(
                        Id: Guid.NewGuid(),
                        OccurredOnUtc: DateTime.UtcNow,
                        TenantId: tenantId,
                        CorrelationId: correlationId,
                        Source: "Chat",
                        ChannelId: channel.Id,
                        ChannelName: channel.Name,
                        MessageId: message.Id,
                        AuthorUserId: currentUserId,
                        MentionedUserId: mentionedUserId,
                        BodyPreview: preview),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return dto;
    }

    /// <summary>Truncate the body for inbox display. Keeps things to a single line, &lt;= 140 chars.</summary>
    private static string MakePreview(string body)
    {
        var collapsed = body.Replace('\r', ' ').Replace('\n', ' ').Trim();
        const int max = 140;
        if (collapsed.Length <= max) return collapsed;
        return string.Concat(collapsed.AsSpan(0, max - 1), "…"); // … ellipsis
    }
}
