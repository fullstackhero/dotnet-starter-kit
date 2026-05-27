using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.DeleteRole;

public sealed record DeleteRoleCommand(string Id) : ICommand<Unit>;