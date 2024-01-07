using FSH.Framework.Core.Identity.Users.Features.RegisterUser;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
public static class RegisterUserEndpoint
{
    internal static RouteHandlerBuilder MapRegisterUserEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/register", (RegisterUserCommand request, ISender mediator) => mediator.Send(request))
                                .WithName(nameof(RegisterUserEndpoint))
                                .WithSummary("register user")
                                .WithDescription("register user");
    }
}