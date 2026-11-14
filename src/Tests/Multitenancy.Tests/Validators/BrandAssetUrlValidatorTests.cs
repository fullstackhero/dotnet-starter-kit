using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.UpdateTenantTheme;
using FSH.Modules.Multitenancy.Features.v1.UpdateTenantTheme;
using Multitenancy.Tests.Support;

namespace Multitenancy.Tests.Validators;

/// <summary>
/// Logo URLs are stored as typed and rendered for every user of the tenant; a file path stored here
/// used to be handed to the storage layer on the next "delete logo".
/// </summary>
public sealed class BrandAssetUrlValidatorTests
{
    private static bool IsValidWithLogo(string? logoUrl)
    {
        var sut = new UpdateTenantThemeCommandValidator(MultitenancyResourcesLocalizerFactory.Create());
        var command = new UpdateTenantThemeCommand(new TenantThemeDto { BrandAssets = new BrandAssetsDto { LogoUrl = logoUrl } });
        return !sut.Validate(command).Errors.Any(e => e.PropertyName.EndsWith(nameof(BrandAssetsDto.LogoUrl), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("https://cdn.example.com/logo.png", true)]
    [InlineData("/public/tenants/acme/logo.png", true)]
    [InlineData("data:image/png;base64,AAAA", true)]
    [InlineData("../../appsettings.json", false)]
    [InlineData("C:\\inetpub\\wwwroot\\web.config", false)]
    [InlineData("\\\\attacker\\share\\logo.png", false)]
    [InlineData("javascript:alert(1)", false)]
    [InlineData("data:text/html,<script>alert(1)</script>", false)]
    public void LogoUrl_Should_AcceptOnlyHttpUrlsSafePathsOrAnImagePreview(string? logoUrl, bool expectedValid)
    {
        IsValidWithLogo(logoUrl).ShouldBe(expectedValid);
    }
}
