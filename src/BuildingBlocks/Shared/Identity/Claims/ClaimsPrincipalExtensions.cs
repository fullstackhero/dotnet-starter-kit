using FSH.Framework.Shared.Constants;
using System.Security.Claims;

namespace FSH.Framework.Shared.Identity.Claims;

public static class ClaimsPrincipalExtensions
{
    // Retrieves the email claim
    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.Email);

    // Retrieves the tenant claim
    public static string? GetTenant(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(CustomClaims.Tenant);

    // Retrieves the user's full name
    public static string? GetFullName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(CustomClaims.Fullname);

    // Retrieves the user's first name
    public static string? GetFirstName(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.Name);

    // Retrieves the user's surname
    public static string? GetSurname(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.Surname);

    // Retrieves the user's phone number
    public static string? GetPhoneNumber(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.MobilePhone);

    // Retrieves the user's ID
    public static string? GetUserId(this ClaimsPrincipal principal) =>
        principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    // Retrieves the user's image URL as Uri
    public static Uri? GetImageUrl(this ClaimsPrincipal principal)
    {
        var imageUrl = principal?.FindFirstValue(CustomClaims.ImageUrl);
        return Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ? uri : null;
    }

    // Retrieves the user's token expiration date
    public static DateTimeOffset GetExpiration(this ClaimsPrincipal principal)
    {
        var expiration = principal?.FindFirstValue(CustomClaims.Expiration);
        return expiration != null
            ? DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(expiration))
            : throw new InvalidOperationException("Expiration claim not found.");
    }

    // Helper method to extract claim value
    private static string? FindFirstValue(this ClaimsPrincipal principal, string claimType) =>
        principal?.FindFirst(claimType)?.Value;
}