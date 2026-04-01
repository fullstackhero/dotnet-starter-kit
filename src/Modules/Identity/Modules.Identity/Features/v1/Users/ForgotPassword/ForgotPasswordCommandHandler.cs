using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Contracts.v1.Users.ForgotPassword;
using Mediator;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Features.v1.Users.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(IUserService userService, IOptions<OriginOptions> originOptions) : ICommandHandler<ForgotPasswordCommand, string>
{
    public async ValueTask<string> Handle(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var origin = originOptions.Value?.OriginUrl?.ToString();
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new InvalidOperationException("Origin URL is not configured.");
        }

        await userService.ForgotPasswordAsync(command.Email, origin, cancellationToken).ConfigureAwait(false);

        return "Password reset email sent.";
    }
}