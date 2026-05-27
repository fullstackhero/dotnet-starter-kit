using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.GetGroupMembers;

public sealed record GetGroupMembersQuery(Guid GroupId) : IQuery<IEnumerable<GroupMemberDto>>;