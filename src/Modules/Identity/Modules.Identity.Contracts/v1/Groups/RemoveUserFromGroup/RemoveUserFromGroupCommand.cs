using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.RemoveUserFromGroup;

public sealed record RemoveUserFromGroupCommand(Guid GroupId, string UserId) : ICommand<Unit>;