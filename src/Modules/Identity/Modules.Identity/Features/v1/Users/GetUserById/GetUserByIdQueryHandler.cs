using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.GetUser;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.GetUserById;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserQuery, UserDto>
{
    private readonly IUserService _userService;

    public GetUserByIdQueryHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _userService.GetAsync(query.Id, cancellationToken).ConfigureAwait(false);
    }
}