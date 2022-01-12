using FSH.WebApi.Infrastructure.Auth.AzureAd;

namespace System.Security.Claims;

public static class GetIssuerExtension
{
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