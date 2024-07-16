using Microsoft.AspNetCore.Routing;

namespace FSH.Framework.Infrastructure.Identity.Users.Endpoints;
internal static class Extensions
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapRegisterUserEndpoint();
        app.MapUpdateUserEndpoint();
        app.MapGetUsersListEndpoint();
        app.MapDeleteUserEndpoint();
        app.MapForgotPasswordEndpoint();
        app.MapResetPasswordEndpoint();
        app.MapGetMeEndpoint();
        app.MapGetUserEndpoint();
        app.MapGetCurrentUserPermissionsEndpoint();
        return app;
    }
}
