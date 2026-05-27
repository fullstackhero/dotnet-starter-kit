using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record DeleteMessageCommand(Guid MessageId) : ICommand<Unit>;
