using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record AddChannelMembersCommand(
    Guid ChannelId,
    IReadOnlyList<string> UserIds) : ICommand<Unit>;
