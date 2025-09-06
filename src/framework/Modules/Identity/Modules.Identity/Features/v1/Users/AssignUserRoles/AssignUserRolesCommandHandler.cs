using FSH.Framework.Identity.Contracts.v1.Users.AssignUserRoles;
using FSH.Framework.Identity.Core.Users;
using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Identity.v1.Users.AssignUserRoles;
internal sealed class AssignUserRolesCommandHandler(IUserService _userService)
    : ICommandHandler<AssignUserRolesCommand, string>
{
    public async Task<string> HandleAsync(
        AssignUserRolesCommand request,
        CancellationToken cancellationToken = default) =>
        await _userService.AssignRolesAsync(request.UserId, request.UserRoles, cancellationToken);
}