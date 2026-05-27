using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.AddUsersToGroup;

public sealed record AddUsersToGroupCommand(Guid GroupId, IReadOnlyList<string> UserIds) : ICommand<AddUsersToGroupResponse>;

public sealed record AddUsersToGroupResponse(int AddedCount, List<string> AlreadyMemberUserIds);