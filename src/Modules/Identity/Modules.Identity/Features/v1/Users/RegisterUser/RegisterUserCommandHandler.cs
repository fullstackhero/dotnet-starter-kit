using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.RegisterUser;

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private readonly IUserService _userService;

    public RegisterUserCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<RegisterUserResponse> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string userId = await _userService.RegisterAsync(
            command.FirstName,
            command.LastName,
            command.Email,
            command.UserName,
            command.Password,
            command.ConfirmPassword,
            command.PhoneNumber ?? string.Empty,
            command.Origin ?? string.Empty,
            cancellationToken).ConfigureAwait(false);

        return new RegisterUserResponse(userId);
    }
}