using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserPermissions;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserPermissions;

public sealed class GetCurrentUserPermissionsQueryHandler : IQueryHandler<GetCurrentUserPermissionsQuery, List<string>?>
{
    private readonly IUserService _userService;

    public GetCurrentUserPermissionsQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<List<string>?> Handle(GetCurrentUserPermissionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _userService.GetPermissionsAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}