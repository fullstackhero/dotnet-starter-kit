#pragma warning disable S1144 // Unused private members — populated by JSON deserialization
#pragma warning disable S3459 // Unassigned members — populated by JSON deserialization
using System.Text.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

/// <summary>
/// End-to-end coverage for the tenant theme feature (get / update / reset) plus
/// validation and cross-tenant isolation. Theme scoping is driven entirely by the
/// resolved Finbuckle tenant context: a root operator can target another tenant by
/// sending the <c>tenant</c> header (root-operator override), while a tenant operator
/// is always pinned to its own tenant (the override is gated to root). These tests
/// pin both the happy paths and the isolation contract — tenant B must never be able
/// to read or mutate tenant A's theme.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class TenantThemeTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private const string ThemePath = $"{TestConstants.TenantsBasePath}/theme";
    private const string ThemeResetPath = $"{TestConstants.TenantsBasePath}/theme/reset";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    private string _tenantA = default!;
    private string _tenantAAdminEmail = default!;
    private string _tenantB = default!;
    private string _tenantBAdminEmail = default!;

    public TenantThemeTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    public async Task InitializeAsync()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        _tenantA = $"theme-a-{unique}";
        _tenantB = $"theme-b-{unique}";
        _tenantAAdminEmail = $"admin-a-{unique}@theme.com";
        _tenantBAdminEmail = $"admin-b-{unique}@theme.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, _tenantA, _tenantAAdminEmail);
        await CreateTenantAsync(rootClient, _tenantB, _tenantBAdminEmail);
        await WaitForProvisioningAsync(rootClient, _tenantA);
        await WaitForProvisioningAsync(rootClient, _tenantB);

        // Ensure both tenant admins are queryable (token issuance is the strongest
        // cross-check that identity seeding finished — see TenantHeaderOverrideTests).
        _ = await GetTokenWithRetryAsync(_tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        _ = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Happy Path

    [Fact]
    public async Task GetTheme_Should_ReturnDefault_When_TenantHasNoCustomTheme()
    {
        // Arrange
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);

        // Act
        var response = await client.GetAsync(ThemePath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var theme = await response.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        theme.ShouldNotBeNull();
        theme.IsDefault.ShouldBeFalse();
        theme.LightPalette.Primary.ShouldBe("#2563EB");
        theme.Typography.FontFamily.ShouldBe("Inter, sans-serif");
        theme.Layout.BorderRadius.ShouldBe("4px");
    }

    [Fact]
    public async Task UpdateTheme_Should_PersistAndReturnUpdatedValues_When_TenantAdminUpdatesOwnTheme()
    {
        // Arrange
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var payload = ValidTheme(primary: "#112233", fontSize: 18, borderRadius: "8px");

        // Act
        var updateResponse = await client.PutAsJsonAsync(ThemePath, payload);

        // Assert
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync(ThemePath);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var theme = await getResponse.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        theme.ShouldNotBeNull();
        theme.LightPalette.Primary.ShouldBe("#112233");
        theme.Typography.FontSizeBase.ShouldBe(18);
        theme.Layout.BorderRadius.ShouldBe("8px");
    }

    [Fact]
    public async Task ResetTheme_Should_RestoreDefaults_After_ThemeWasCustomized()
    {
        // Arrange — customize first
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var update = await client.PutAsJsonAsync(ThemePath, ValidTheme(primary: "#AABBCC", fontSize: 20));
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Act — reset
        var resetResponse = await client.PostAsync(ThemeResetPath, content: null);

        // Assert
        resetResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var getResponse = await client.GetAsync(ThemePath);
        var theme = await getResponse.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        theme.ShouldNotBeNull();
        theme.LightPalette.Primary.ShouldBe("#2563EB");
        theme.Typography.FontSizeBase.ShouldBe(14);
    }

    #endregion

    #region Validation

    [Fact]
    public async Task UpdateTheme_Should_Return400_When_PaletteColorIsNotHex()
    {
        // Arrange
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var payload = ValidTheme(primary: "not-a-color");

        // Act
        var response = await client.PutAsJsonAsync(ThemePath, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTheme_Should_Return400_When_FontSizeOutOfRange()
    {
        // Arrange — FontSizeBase must be between 10 and 24
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var payload = ValidTheme(fontSize: 99);

        // Act
        var response = await client.PutAsJsonAsync(ThemePath, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTheme_Should_Return400_When_FontFamilyIsNotWebSafe()
    {
        // Arrange
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var payload = ValidTheme(fontFamily: "Comic Sans MS, cursive");

        // Act
        var response = await client.PutAsJsonAsync(ThemePath, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateTheme_Should_Return400_When_BorderRadiusIsInvalidCss()
    {
        // Arrange — BorderRadius must match ^\d+(px|rem|em|%)$
        using var client = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var payload = ValidTheme(borderRadius: "round");

        // Act
        var response = await client.PutAsJsonAsync(ThemePath, payload);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region AuthZ

    [Fact]
    public async Task GetTheme_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", _tenantA);

        // Act
        var response = await client.GetAsync(ThemePath);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTheme_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", _tenantA);

        // Act
        var response = await client.PutAsJsonAsync(ThemePath, ValidTheme());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Cross-Tenant Isolation

    [Fact]
    public async Task UpdateTheme_Should_NotLeakAcrossTenants_When_RootOperatorTargetsTenantA()
    {
        // Arrange — root operator scopes to tenant A via the header override and
        // sets a distinctive primary color.
        var rootToken = await _auth.GetRootAdminTokenAsync();
        const string marker = "#9911AA";

        using (var clientA = _factory.CreateClient())
        {
            clientA.DefaultRequestHeaders.Authorization = new("Bearer", rootToken.AccessToken);
            clientA.DefaultRequestHeaders.Add("tenant", _tenantA);
            var update = await clientA.PutAsJsonAsync(ThemePath, ValidTheme(primary: marker));
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Act — root operator now scopes to tenant B and reads its theme.
        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new("Bearer", rootToken.AccessToken);
        clientB.DefaultRequestHeaders.Add("tenant", _tenantB);
        var responseB = await clientB.GetAsync(ThemePath);

        // Assert — tenant B must NOT see tenant A's customization.
        responseB.StatusCode.ShouldBe(HttpStatusCode.OK);
        var themeB = await responseB.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        themeB.ShouldNotBeNull();
        themeB.LightPalette.Primary.ShouldNotBe(marker);
        themeB.LightPalette.Primary.ShouldBe("#2563EB");
    }

    [Fact]
    public async Task GetTheme_Should_StayInOwnTenant_When_TenantBAdminSendsTenantAHeader()
    {
        // Arrange — give tenant A a distinctive theme (as A's own admin).
        const string marker = "#7733EE";
        using (var clientA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA))
        {
            var update = await clientA.PutAsJsonAsync(ThemePath, ValidTheme(primary: marker));
            update.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Tenant B admin tries to read tenant A's theme by spoofing the header.
        var tokenB = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
        using var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new("Bearer", tokenB.AccessToken);
        clientB.DefaultRequestHeaders.Add("tenant", _tenantA); // spoof attempt — must be ignored

        // Act
        var response = await clientB.GetAsync(ThemePath);

        // Assert — the override is gated to root, so B stays in B and sees defaults,
        // never tenant A's marker color.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var theme = await response.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        theme.ShouldNotBeNull();
        theme.LightPalette.Primary.ShouldNotBe(marker);
        theme.LightPalette.Primary.ShouldBe("#2563EB");
    }

    [Fact]
    public async Task UpdateTheme_Should_NotMutateTenantA_When_TenantBAdminSendsTenantAHeader()
    {
        // Arrange — tenant A's admin sets a known baseline.
        const string baseline = "#445566";
        using (var clientA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA))
        {
            var seed = await clientA.PutAsJsonAsync(ThemePath, ValidTheme(primary: baseline));
            seed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Tenant B admin tries to overwrite tenant A's theme by spoofing the header.
        var tokenB = await GetTokenWithRetryAsync(_tenantBAdminEmail, TestConstants.DefaultPassword, _tenantB);
        using (var clientB = _factory.CreateClient())
        {
            clientB.DefaultRequestHeaders.Authorization = new("Bearer", tokenB.AccessToken);
            clientB.DefaultRequestHeaders.Add("tenant", _tenantA); // spoof attempt
            var attack = await clientB.PutAsJsonAsync(ThemePath, ValidTheme(primary: "#000000"));
            // The write is accepted but applies to tenant B (where B is pinned), not A.
            attack.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        // Act — re-read tenant A's theme as A's own admin.
        using var verifyA = await _auth.CreateAuthenticatedClientAsync(
            _tenantAAdminEmail, TestConstants.DefaultPassword, _tenantA);
        var response = await verifyA.GetAsync(ThemePath);

        // Assert — tenant A's theme is unchanged by B's spoofed write.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var theme = await response.Content.ReadFromJsonAsync<TenantThemeDto>(Json);
        theme.ShouldNotBeNull();
        theme.LightPalette.Primary.ShouldBe(baseline);
    }

    #endregion

    #region Helpers

    private static object ValidTheme(
        string primary = "#2563EB",
        double fontSize = 14,
        string fontFamily = "Inter, sans-serif",
        string borderRadius = "4px")
    {
        return new
        {
            lightPalette = new
            {
                primary,
                secondary = "#0F172A",
                tertiary = "#6366F1",
                background = "#F8FAFC",
                surface = "#FFFFFF",
                error = "#DC2626",
                warning = "#F59E0B",
                success = "#16A34A",
                info = "#0284C7"
            },
            darkPalette = new
            {
                primary = "#38BDF8",
                secondary = "#94A3B8",
                tertiary = "#818CF8",
                background = "#0B1220",
                surface = "#111827",
                error = "#F87171",
                warning = "#FBBF24",
                success = "#22C55E",
                info = "#38BDF8"
            },
            brandAssets = new { },
            typography = new
            {
                fontFamily,
                headingFontFamily = "Inter, sans-serif",
                fontSizeBase = fontSize,
                lineHeightBase = 1.5
            },
            layout = new
            {
                borderRadius,
                defaultElevation = 1
            }
        };
    }

    private async Task<TokenResult> GetTokenWithRetryAsync(string email, string password, string tenant, int maxRetries = 30)
    {
        Exception? last = null;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.GetTokenAsync(email, password, tenant);
            }
            catch (HttpRequestException ex)
            {
                last = ex;
                await Task.Delay(500);
            }
        }
        throw last ?? new InvalidOperationException("token issuance failed");
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Theme {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            adminPassword = TestConstants.DefaultPassword,
            issuer = $"{tenantId}.issuer",
        });
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.ShouldBe(HttpStatusCode.Created, $"Create tenant failed: {body}");
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }

    // Local copy of the theme response shape — only the fields these tests assert on.
    private sealed record TenantThemeDto
    {
        public PaletteDto LightPalette { get; init; } = new();
        public PaletteDto DarkPalette { get; init; } = new();
        public TypographyDto Typography { get; init; } = new();
        public LayoutDto Layout { get; init; } = new();
        public bool IsDefault { get; init; }
    }

    private sealed record PaletteDto
    {
        public string Primary { get; init; } = string.Empty;
        public string Secondary { get; init; } = string.Empty;
    }

    private sealed record TypographyDto
    {
        public string FontFamily { get; init; } = string.Empty;
        public double FontSizeBase { get; init; }
    }

    private sealed record LayoutDto
    {
        public string BorderRadius { get; init; } = string.Empty;
        public int DefaultElevation { get; init; }
    }

    #endregion
}
