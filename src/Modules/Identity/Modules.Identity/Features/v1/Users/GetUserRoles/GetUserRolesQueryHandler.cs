using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserRoles;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserRoles;

public sealed class GetUserRolesQueryHandler(IUserService userService) : IQueryHandler<GetUserRolesQuery, List<UserRoleDto>>
{
    public async ValueTask<List<UserRoleDto>> Handle(GetUserRolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetUserRolesAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}