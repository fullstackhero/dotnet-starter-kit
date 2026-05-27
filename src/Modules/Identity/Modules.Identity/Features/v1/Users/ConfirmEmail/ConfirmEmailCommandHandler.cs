using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ConfirmEmail;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand, string>
{
    private readonly IUserService _userService;

    public ConfirmEmailCommandHandler(IUserService userService)
    {
        _userService = userService;
    }

    public async ValueTask<string> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        return await _userService.ConfirmEmailAsync(command.UserId, command.Code, command.Tenant, cancellationToken)
            .ConfigureAwait(false);
    }
}