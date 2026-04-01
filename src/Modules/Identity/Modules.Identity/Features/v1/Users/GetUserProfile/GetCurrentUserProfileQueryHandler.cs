using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUserProfile;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserProfile;

public sealed class GetCurrentUserProfileQueryHandler(IUserService userService) : IQueryHandler<GetCurrentUserProfileQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetCurrentUserProfileQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetAsync(query.UserId, cancellationToken).ConfigureAwait(false);
    }
}