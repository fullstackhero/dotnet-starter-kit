using FSH.Modules.Multitenancy.Domain;

namespace Multitenancy.Tests.Domain;

/// <summary>
/// Tests for TenantTheme domain entity - theme configuration per tenant.
/// </summary>
public sealed class TenantThemeTests
{
    #region Create Factory Method Tests

    [Fact]
    public void Create_Should_SetTenantId()
    {
        // Arrange
        var tenantId = "tenant-1";

        // Act
        var theme = TenantTheme.Create(tenantId);

        // Assert
        theme.TenantId.ShouldBe(tenantId);
    }

    [Fact]
    public void Create_Should_GenerateNewId()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_Should_SetCreatedBy_When_Provided()
    {
        // Arrange
        var createdBy = "user-123";

        // Act
        var theme = TenantTheme.Create("tenant-1", createdBy);

        // Assert
        theme.CreatedBy.ShouldBe(createdBy);
    }

    [Fact]
    public void Create_Should_AllowNullCreatedBy()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.CreatedBy.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_SetCreatedOnUtc()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var theme = TenantTheme.Create("tenant-1");
        var after = DateTimeOffset.UtcNow;

        // Assert
        theme.CreatedOnUtc.ShouldBeGreaterThanOrEqualTo(before);
        theme.CreatedOnUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_Should_InitializeDefaultLightPalette()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.PrimaryColor.ShouldBe("#2563EB");
        theme.SecondaryColor.ShouldBe("#0F172A");
        theme.TertiaryColor.ShouldBe("#6366F1");
        theme.BackgroundColor.ShouldBe("#F8FAFC");
        theme.SurfaceColor.ShouldBe("#FFFFFF");
        theme.ErrorColor.ShouldBe("#DC2626");
        theme.WarningColor.ShouldBe("#F59E0B");
        theme.SuccessColor.ShouldBe("#16A34A");
        theme.InfoColor.ShouldBe("#0284C7");
    }

    [Fact]
    public void Create_Should_InitializeDefaultDarkPalette()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.DarkPrimaryColor.ShouldBe("#38BDF8");
        theme.DarkSecondaryColor.ShouldBe("#94A3B8");
        theme.DarkTertiaryColor.ShouldBe("#818CF8");
        theme.DarkBackgroundColor.ShouldBe("#0B1220");
        theme.DarkSurfaceColor.ShouldBe("#111827");
        theme.DarkErrorColor.ShouldBe("#F87171");
        theme.DarkWarningColor.ShouldBe("#FBBF24");
        theme.DarkSuccessColor.ShouldBe("#22C55E");
        theme.DarkInfoColor.ShouldBe("#38BDF8");
    }

    [Fact]
    public void Create_Should_InitializeDefaultTypography()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.FontFamily.ShouldBe("Inter, sans-serif");
        theme.HeadingFontFamily.ShouldBe("Inter, sans-serif");
        theme.FontSizeBase.ShouldBe(14);
        theme.LineHeightBase.ShouldBe(1.5);
    }

    [Fact]
    public void Create_Should_InitializeDefaultLayout()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.BorderRadius.ShouldBe("4px");
        theme.DefaultElevation.ShouldBe(1);
    }

    [Fact]
    public void Create_Should_InitializeNullBrandAssets()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.LogoUrl.ShouldBeNull();
        theme.LogoDarkUrl.ShouldBeNull();
        theme.FaviconUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_InitializeIsDefaultFalse()
    {
        // Act
        var theme = TenantTheme.Create("tenant-1");

        // Assert
        theme.IsDefault.ShouldBeFalse();
    }

    #endregion

    #region Update Method Tests

    [Fact]
    public void Update_Should_SetLastModifiedOnUtc()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        var before = DateTimeOffset.UtcNow;

        // Act
        theme.Update("modifier-user");
        var after = DateTimeOffset.UtcNow;

        // Assert
        theme.LastModifiedOnUtc.ShouldNotBeNull();
        theme.LastModifiedOnUtc.Value.ShouldBeGreaterThanOrEqualTo(before);
        theme.LastModifiedOnUtc.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Update_Should_SetLastModifiedBy()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        var modifiedBy = "modifier-user";

        // Act
        theme.Update(modifiedBy);

        // Assert
        theme.LastModifiedBy.ShouldBe(modifiedBy);
    }

    [Fact]
    public void Update_Should_AllowNullModifier()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");

        // Act
        theme.Update(null);

        // Assert
        theme.LastModifiedBy.ShouldBeNull();
        theme.LastModifiedOnUtc.ShouldNotBeNull();
    }

    #endregion

    #region ResetToDefaults Method Tests

    [Fact]
    public void ResetToDefaults_Should_ResetLightPalette()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.PrimaryColor = "#FF0000";
        theme.SecondaryColor = "#00FF00";
        theme.TertiaryColor = "#0000FF";
        theme.BackgroundColor = "#AAAAAA";
        theme.SurfaceColor = "#BBBBBB";
        theme.ErrorColor = "#CCCCCC";
        theme.WarningColor = "#DDDDDD";
        theme.SuccessColor = "#EEEEEE";
        theme.InfoColor = "#FFFFFF";

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.PrimaryColor.ShouldBe("#2563EB");
        theme.SecondaryColor.ShouldBe("#0F172A");
        theme.TertiaryColor.ShouldBe("#6366F1");
        theme.BackgroundColor.ShouldBe("#F8FAFC");
        theme.SurfaceColor.ShouldBe("#FFFFFF");
        theme.ErrorColor.ShouldBe("#DC2626");
        theme.WarningColor.ShouldBe("#F59E0B");
        theme.SuccessColor.ShouldBe("#16A34A");
        theme.InfoColor.ShouldBe("#0284C7");
    }

    [Fact]
    public void ResetToDefaults_Should_ResetDarkPalette()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.DarkPrimaryColor = "#FF0000";
        theme.DarkSecondaryColor = "#00FF00";
        theme.DarkTertiaryColor = "#0000FF";

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.DarkPrimaryColor.ShouldBe("#38BDF8");
        theme.DarkSecondaryColor.ShouldBe("#94A3B8");
        theme.DarkTertiaryColor.ShouldBe("#818CF8");
        theme.DarkBackgroundColor.ShouldBe("#0B1220");
        theme.DarkSurfaceColor.ShouldBe("#111827");
        theme.DarkErrorColor.ShouldBe("#F87171");
        theme.DarkWarningColor.ShouldBe("#FBBF24");
        theme.DarkSuccessColor.ShouldBe("#22C55E");
        theme.DarkInfoColor.ShouldBe("#38BDF8");
    }

    [Fact]
    public void ResetToDefaults_Should_ClearBrandAssets()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.LogoUrl = "https://example.com/logo.png";
        theme.LogoDarkUrl = "https://example.com/logo-dark.png";
        theme.FaviconUrl = "https://example.com/favicon.ico";

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.LogoUrl.ShouldBeNull();
        theme.LogoDarkUrl.ShouldBeNull();
        theme.FaviconUrl.ShouldBeNull();
    }

    [Fact]
    public void ResetToDefaults_Should_ResetTypography()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.FontFamily = "Roboto, sans-serif";
        theme.HeadingFontFamily = "Montserrat, sans-serif";
        theme.FontSizeBase = 18;
        theme.LineHeightBase = 2.0;

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.FontFamily.ShouldBe("Inter, sans-serif");
        theme.HeadingFontFamily.ShouldBe("Inter, sans-serif");
        theme.FontSizeBase.ShouldBe(14);
        theme.LineHeightBase.ShouldBe(1.5);
    }

    [Fact]
    public void ResetToDefaults_Should_ResetLayout()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.BorderRadius = "8px";
        theme.DefaultElevation = 5;

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.BorderRadius.ShouldBe("4px");
        theme.DefaultElevation.ShouldBe(1);
    }

    [Fact]
    public void ResetToDefaults_Should_NotResetTenantId()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.TenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void ResetToDefaults_Should_NotResetId()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        var originalId = theme.Id;

        // Act
        theme.ResetToDefaults();

        // Assert
        theme.Id.ShouldBe(originalId);
    }

    [Fact]
    public void ResetToDefaults_Should_NotResetIsDefault()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");
        theme.IsDefault = true;

        // Act
        theme.ResetToDefaults();

        // Assert - IsDefault is not reset by ResetToDefaults
        theme.IsDefault.ShouldBeTrue();
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_Should_BeSettable()
    {
        // Arrange
        var theme = TenantTheme.Create("tenant-1");

        // Act
        theme.PrimaryColor = "#CUSTOM1";
        theme.IsDefault = true;
        theme.LogoUrl = "https://example.com/logo.png";

        // Assert
        theme.PrimaryColor.ShouldBe("#CUSTOM1");
        theme.IsDefault.ShouldBeTrue();
        theme.LogoUrl.ShouldBe("https://example.com/logo.png");
    }

    [Fact]
    public void Create_Should_GenerateUniqueIds()
    {
        // Act
        var theme1 = TenantTheme.Create("tenant-1");
        var theme2 = TenantTheme.Create("tenant-2");

        // Assert
        theme1.Id.ShouldNotBe(theme2.Id);
    }

    #endregion
}