using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUser;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserById;

public sealed class GetUserByIdQueryHandler(IUserService userService) : IQueryHandler<GetUserQuery, UserDto>
{
    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await userService.GetAsync(query.Id, cancellationToken).ConfigureAwait(false);
    }
}