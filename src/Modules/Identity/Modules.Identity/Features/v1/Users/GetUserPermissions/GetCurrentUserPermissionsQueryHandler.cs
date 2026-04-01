using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserPermissions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserPermissions;

public sealed class GetCurrentUserPermissionsQueryHandler(IUserService userService) : IQueryHandler<GetCurrentUserPermissionsQuery, List<string>?>
{
    public async ValueTask<List<string>?> Handle(GetCurrentUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetPermissionsAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}