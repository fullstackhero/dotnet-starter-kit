using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record RestoreChannelCommand(Guid ChannelId) : ICommand<Unit>;
