using FSH.Modules.Chat.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record SendMessageAttachmentInput(
    Guid? FileAssetId,
    string Url,
    string ContentType,
    string FileName,
    long SizeBytes);

public sealed record SendMessageCommand(
    Guid ChannelId,
    string Body,
    Guid? ParentMessageId,
    IReadOnlyList<SendMessageAttachmentInput> Attachments) : ICommand<MessageDto>;
