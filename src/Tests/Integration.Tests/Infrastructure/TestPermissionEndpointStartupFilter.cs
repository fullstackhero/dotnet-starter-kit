using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Integration.Tests.Infrastructure;

/// <summary>
/// No production endpoint passes more than one permission to <c>.RequirePermission()</c>, so this
/// maps one that does. It appends to the live route builder after <c>next(app)</c> has run
/// <c>UseRouting()</c>, the same way <c>Integration.Middleware.Tests</c> maps <c>/__test/throw</c>.
/// </summary>
public sealed class TestPermissionEndpointStartupFilter : IStartupFilter
{
    public const string TwoPermissionsPath = "/__test/permissions/two";
    public const string FirstPermission = IdentityPermissions.Groups.Create;
    public const string SecondPermission = IdentityPermissions.Groups.Delete;

    private const string EndpointRouteBuilderKey = "__EndpointRouteBuilder";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return app =>
        {
            next(app);

            if (app.Properties.TryGetValue(EndpointRouteBuilderKey, out var value)
                && value is IEndpointRouteBuilder routeBuilder)
            {
                routeBuilder.MapGet(TwoPermissionsPath, () => Results.Ok())
                    .RequireAuthorization()
                    .RequirePermission(FirstPermission, SecondPermission);
            }
        };
    }
}
