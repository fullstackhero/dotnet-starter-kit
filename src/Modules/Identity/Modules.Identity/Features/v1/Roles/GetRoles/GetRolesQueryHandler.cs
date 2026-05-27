using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Roles.GetRoles;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Roles.GetRoles;

public sealed class GetRolesQueryHandler : IQueryHandler<GetRolesQuery, PagedResponse<RoleDto>>
{
    private readonly IRoleService _roleService;

    public GetRolesQueryHandler(IRoleService roleService)
    {
        _roleService = roleService;
    }

    public async ValueTask<PagedResponse<RoleDto>> Handle(GetRolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _roleService.GetRolesAsync(
            query.PageNumber ?? 1,
            query.PageSize ?? 20,
            query.Search,
            cancellationToken).ConfigureAwait(false);
    }
}
