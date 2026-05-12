using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record UpdateChannelCommand(
    Guid ChannelId,
    string Name,
    string? Description,
    bool IsPrivate) : ICommand<Unit>;
