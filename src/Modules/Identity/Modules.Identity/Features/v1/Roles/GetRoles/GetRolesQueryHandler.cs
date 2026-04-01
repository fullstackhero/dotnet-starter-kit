using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.GetRoles;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Roles.GetRoles;

public sealed class GetRolesQueryHandler(IRoleService roleService) : IQueryHandler<GetRolesQuery, IEnumerable<RoleDto>>
{
    public async ValueTask<IEnumerable<RoleDto>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        return await roleService.GetRolesAsync(cancellationToken).ConfigureAwait(false);
    }
}