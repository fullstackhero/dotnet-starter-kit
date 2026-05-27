using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.DeleteGroup;

public sealed record DeleteGroupCommand(Guid Id) : ICommand<Unit>;