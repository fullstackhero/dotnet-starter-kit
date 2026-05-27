using FSH.Modules.Identity.Data;

namespace Identity.Tests.Data;

/// <summary>
/// Tests for PasswordPolicyOptions - configuration for password policies.
/// </summary>
public sealed class PasswordPolicyOptionsTests
{
    [Fact]
    public void DefaultValues_Should_BeSecure()
    {
        // Arrange & Act
        var options = new PasswordPolicyOptions();

        // Assert - Verify secure defaults
        options.PasswordHistoryCount.ShouldBe(5);
        options.PasswordExpiryDays.ShouldBe(90);
        options.PasswordExpiryWarningDays.ShouldBe(14);
        options.EnforcePasswordExpiry.ShouldBeTrue();
    }

    [Fact]
    public void PasswordHistoryCount_Should_BeSettable()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            PasswordHistoryCount = 10
        };

        // Assert
        options.PasswordHistoryCount.ShouldBe(10);
    }

    [Fact]
    public void PasswordExpiryDays_Should_BeSettable()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            PasswordExpiryDays = 30
        };

        // Assert
        options.PasswordExpiryDays.ShouldBe(30);
    }

    [Fact]
    public void PasswordExpiryWarningDays_Should_BeSettable()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            PasswordExpiryWarningDays = 7
        };

        // Assert
        options.PasswordExpiryWarningDays.ShouldBe(7);
    }

    [Fact]
    public void EnforcePasswordExpiry_Should_BeSettable()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = false
        };

        // Assert
        options.EnforcePasswordExpiry.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(100)]
    public void PasswordHistoryCount_Should_AcceptAnyInteger(int value)
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            PasswordHistoryCount = value
        };

        // Assert
        options.PasswordHistoryCount.ShouldBe(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(365)]
    [InlineData(730)]
    public void PasswordExpiryDays_Should_AcceptAnyInteger(int value)
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            PasswordExpiryDays = value
        };

        // Assert
        options.PasswordExpiryDays.ShouldBe(value);
    }
}