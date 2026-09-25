using FSH.Framework.Web.Frontend;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using Mediator;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, string>
{
    private readonly IUserService _userService;
    private readonly IFrontendOriginResolver _originResolver;

    public ForgotPasswordCommandHandler(IUserService userService, IFrontendOriginResolver originResolver)
    {
        _userService = userService;
        _originResolver = originResolver;
    }

    public async ValueTask<string> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Self-service flow: the reset link must land on the SPA the user is currently using.
        var origin = _originResolver.ResolveForCurrentRequest();

        await _userService.ForgotPasswordAsync(command.Email, origin, cancellationToken).ConfigureAwait(false);

        return "Password reset email sent.";
    }
}