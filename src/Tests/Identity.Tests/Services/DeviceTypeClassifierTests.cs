using FSH.Modules.Identity.Services;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for DeviceTypeClassifier - pure user-agent device-family to device-type parser.
/// </summary>
public sealed class DeviceTypeClassifierTests
{
    #region Desktop (Default / Edge Cases)

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Other")]
    public void Classify_Should_ReturnDesktop_When_DeviceFamilyIsNullEmptyOrOther(string? deviceFamily)
    {
        // Act
        var result = DeviceTypeClassifier.Classify(deviceFamily);

        // Assert
        result.ShouldBe("Desktop");
    }

    [Theory]
    [InlineData("Mac")]
    [InlineData("Windows")]
    [InlineData("Linux")]
    [InlineData("Chromebook")]
    public void Classify_Should_ReturnDesktop_When_DeviceFamilyIsUnrecognized(string deviceFamily)
    {
        // Act
        var result = DeviceTypeClassifier.Classify(deviceFamily);

        // Assert
        result.ShouldBe("Desktop");
    }

    #endregion

    #region Mobile

    [Theory]
    [InlineData("Mobile")]
    [InlineData("iPhone")]
    [InlineData("Android Phone")]
    [InlineData("Generic Smartphone")]
    public void Classify_Should_ReturnMobile_When_DeviceFamilyContainsMobileKeyword(string deviceFamily)
    {
        // Act
        var result = DeviceTypeClassifier.Classify(deviceFamily);

        // Assert
        result.ShouldBe("Mobile");
    }

    [Fact]
    public void Classify_Should_ReturnMobile_When_KeywordCasingDiffers()
    {
        // Act - classification is case-insensitive (input is normalized to lower-invariant)
        var result = DeviceTypeClassifier.Classify("IPHONE");

        // Assert
        result.ShouldBe("Mobile");
    }

    #endregion

    #region Tablet

    [Theory]
    [InlineData("Tablet")]
    [InlineData("iPad")]
    [InlineData("Samsung Tablet")]
    public void Classify_Should_ReturnTablet_When_DeviceFamilyContainsTabletKeyword(string deviceFamily)
    {
        // Act
        var result = DeviceTypeClassifier.Classify(deviceFamily);

        // Assert
        result.ShouldBe("Tablet");
    }

    [Fact]
    public void Classify_Should_PreferMobile_When_FamilyMatchesBothMobileAndTabletKeywords()
    {
        // Arrange - mobile keywords are evaluated before tablet keywords
        // "android tablet" contains the mobile keyword "android" and the tablet keyword "tablet".

        // Act
        var result = DeviceTypeClassifier.Classify("Android Tablet");

        // Assert
        result.ShouldBe("Mobile");
    }

    #endregion
}
