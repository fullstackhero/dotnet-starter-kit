using DN.WebApi.Domain.Constants;
using DN.WebApi.Infrastructure.Identity.AzureAd;
using System.Security.Claims;

namespace DN.WebApi.Infrastructure.Identity.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetUserEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email);

    public static string? GetTenant(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimConstants.Tenant);

    public static string? GetIssuer(this ClaimsPrincipal principal)
    {
        if (principal.FindFirstValue(OpenIdConnectClaimTypes.Issuer) is string issuer)
        {
            return issuer;
        }

        // Workaround to deal with missing "iss" claim. We search for the ObjectId claim instead and return the value of Issuer property of that Claim
        return principal.FindFirst(AzureADClaimTypes.ObjectId)?.Issuer;
    }
}