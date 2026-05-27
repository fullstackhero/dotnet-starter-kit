using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.GetRole;

public sealed record GetRoleQuery(string Id) : IQuery<RoleDto?>;