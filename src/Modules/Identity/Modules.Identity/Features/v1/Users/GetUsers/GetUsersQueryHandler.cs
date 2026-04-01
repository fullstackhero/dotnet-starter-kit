using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUsers;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUsers;

public sealed class GetUsersQueryHandler(IUserService userService) : IQueryHandler<GetUsersQuery, List<UserDto>>
{
    public async ValueTask<List<UserDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        return await userService.GetListAsync(cancellationToken).ConfigureAwait(false);
    }
}