using Mediator;

namespace FSH.Modules.Chat.Contracts.v1.Commands;

public sealed record EditMessageCommand(Guid MessageId, string Body) : ICommand<Unit>;
