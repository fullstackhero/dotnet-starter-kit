using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Roles.GetRoleWithPermissions;

public sealed record GetRoleWithPermissionsQuery(string Id) : IQuery<RoleDto>;