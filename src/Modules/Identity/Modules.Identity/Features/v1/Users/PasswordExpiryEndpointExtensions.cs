using FSH.Modules.Identity.Features.v1.Users.PasswordExpiry;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Users;

public static class PasswordExpiryEndpointExtensions
{
    public static IEndpointRouteBuilder MapGetPasswordExpiryStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        GetPasswordExpiryStatusEndpoint.Map(endpoints);
        return endpoints;
    }
}
