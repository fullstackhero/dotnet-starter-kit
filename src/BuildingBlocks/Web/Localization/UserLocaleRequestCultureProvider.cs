using System.Security.Claims;
using FSH.Framework.Core.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace FSH.Framework.Web.Localization;

/// <summary>
/// Resolves the request culture from the authenticated user's <c>locale</c> claim
/// (mirrors the persisted <c>User.Locale</c>). Sits after the query-string provider and before
/// the Accept-Language provider: a supported claim wins over the browser header, an unsupported
/// or absent claim falls through to the next provider.
/// </summary>
public sealed class UserLocaleRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var claim = httpContext.User.FindFirstValue("locale");
        if (IsSupported(claim))
        {
            return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(claim!));
        }

        return NullProviderCultureResult;
    }

    private static bool IsSupported(string? tag) =>
        !string.IsNullOrWhiteSpace(tag) && SupportedCultures.Tags.Contains(tag);
}
