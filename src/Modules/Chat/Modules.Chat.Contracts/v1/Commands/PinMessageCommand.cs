using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record PinMessageCommand(Guid MessageId) : ICommand<Unit>;

public sealed record UnpinMessageCommand(Guid MessageId) : ICommand<Unit>;
