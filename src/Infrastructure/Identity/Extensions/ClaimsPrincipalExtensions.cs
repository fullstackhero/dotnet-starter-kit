using System.Security.Claims;

namespace DN.WebApi.Infrastructure.Identity.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        // throw exception if principle is null
        ArgumentNullException.ThrowIfNull(principal);

        var claim = principal.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }

    public static string? GetUserEmail(this ClaimsPrincipal principal)
    {
        // throw exception if principle is null
        ArgumentNullException.ThrowIfNull(principal);

        var claim = principal.FindFirst(ClaimTypes.Email);
        return claim?.Value;
    }

    public static string? GetTenant(this ClaimsPrincipal principal)
    {
        // throw exception if principle is null
        ArgumentNullException.ThrowIfNull(principal);

        var claim = principal.FindFirst("tenant");
        return claim?.Value;
    }
}