using FSH.Framework.Core.Identity.Users.Abstractions;
using MediatR;

namespace FSH.Framework.Core.Identity.Users.Features.RegisterUser;
public sealed class RegisterUserHandler(IUserService service) : IRequestHandler<RegisterUserCommand, RegisterUserResponse>
{
    public Task<RegisterUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        return service.RegisterAsync(request, cancellationToken);
    }
}
