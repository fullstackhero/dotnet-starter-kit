using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Groups.GetGroupById;

public sealed record GetGroupByIdQuery(Guid Id) : IQuery<GroupDto>;