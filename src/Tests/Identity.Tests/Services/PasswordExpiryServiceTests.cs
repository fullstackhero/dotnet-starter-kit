using FSH.Modules.Identity.Contracts.Services;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Identity.Tests.Services;

/// <summary>
/// Tests for PasswordExpiryService - handles password expiry logic.
/// </summary>
public sealed class PasswordExpiryServiceTests
{
    private readonly UserManager<FshUser> _userManager;

    public PasswordExpiryServiceTests()
    {
        var userStore = Substitute.For<IUserStore<FshUser>>();
        _userManager = Substitute.For<UserManager<FshUser>>(
            userStore, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private PasswordExpiryService CreateService(PasswordPolicyOptions options)
    {
        return new PasswordExpiryService(_userManager, Options.Create(options), TimeProvider.System);
    }

    private static FshUser CreateUser(DateTime lastPasswordChangeDate)
    {
        return new FshUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "testuser",
            LastPasswordChangeDate = lastPasswordChangeDate
        };
    }

    private void SetupUserManager(FshUser user)
    {
        _userManager.FindByIdAsync(user.Id).Returns(user);
    }

    #region IsPasswordExpiredAsync Tests

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnFalse_When_EnforcePasswordExpiryIsFalse()
    {
        // Arrange
        var options = new PasswordPolicyOptions { EnforcePasswordExpiry = false };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-1000)); // Very old password
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiredAsync(user.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnTrue_When_PasswordExceedsExpiryDays()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-91)); // Password changed 91 days ago
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiredAsync(user.Id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnFalse_When_PasswordWithinExpiryDays()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-89)); // Password changed 89 days ago
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiredAsync(user.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnFalse_When_PasswordChangedToday()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow);
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiredAsync(user.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnTrue_When_ExactlyOnExpiryBoundary()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        // Password changed exactly 90 days and 1 second ago (just past expiry)
        var user = CreateUser(DateTime.UtcNow.AddDays(-90).AddSeconds(-1));
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiredAsync(user.Id);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsPasswordExpiredAsync_Should_ReturnFalse_When_UserNotFound()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((FshUser?)null);

        // Act
        var result = await service.IsPasswordExpiredAsync("nonexistent-user-id");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetDaysUntilExpiryAsync Tests

    [Fact]
    public async Task GetDaysUntilExpiryAsync_Should_ReturnMaxValue_When_EnforcePasswordExpiryIsFalse()
    {
        // Arrange
        var options = new PasswordPolicyOptions { EnforcePasswordExpiry = false };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-1000));
        SetupUserManager(user);

        // Act
        var result = await service.GetDaysUntilExpiryAsync(user.Id);

        // Assert
        result.ShouldBe(int.MaxValue);
    }

    [Fact]
    public async Task GetDaysUntilExpiryAsync_Should_ReturnPositiveDays_When_PasswordNotExpired()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-80)); // 80 days ago
        SetupUserManager(user);

        // Act
        var result = await service.GetDaysUntilExpiryAsync(user.Id);

        // Assert - TotalDays truncates, so could be 9 or 10 depending on time of day
        result.ShouldBeInRange(9, 10);
    }

    [Fact]
    public async Task GetDaysUntilExpiryAsync_Should_ReturnNegativeDays_When_PasswordExpired()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-100)); // 100 days ago
        SetupUserManager(user);

        // Act
        var result = await service.GetDaysUntilExpiryAsync(user.Id);

        // Assert
        result.ShouldBeLessThan(0); // Expired 10 days ago
    }

    [Fact]
    public async Task GetDaysUntilExpiryAsync_Should_ReturnExpiryDays_When_PasswordJustChanged()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow);
        SetupUserManager(user);

        // Act
        var result = await service.GetDaysUntilExpiryAsync(user.Id);

        // Assert - TotalDays truncates, so could be 89 or 90 depending on time of day
        result.ShouldBeInRange(89, 90);
    }

    [Fact]
    public async Task GetDaysUntilExpiryAsync_Should_ReturnMaxValue_When_UserNotFound()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((FshUser?)null);

        // Act
        var result = await service.GetDaysUntilExpiryAsync("nonexistent-user-id");

        // Assert
        result.ShouldBe(int.MaxValue);
    }

    #endregion

    #region IsPasswordExpiringWithinWarningPeriodAsync Tests

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnFalse_When_EnforcePasswordExpiryIsFalse()
    {
        // Arrange
        var options = new PasswordPolicyOptions { EnforcePasswordExpiry = false };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-85));
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync(user.Id);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnTrue_When_WithinWarningDays()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-80)); // 10 days until expiry
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync(user.Id);

        // Assert
        result.ShouldBeTrue(); // 10 days <= 14 warning days
    }

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnFalse_When_OutsideWarningDays()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-70)); // 20 days until expiry
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync(user.Id);

        // Assert
        result.ShouldBeFalse(); // 20 days > 14 warning days
    }

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnFalse_When_AlreadyExpired()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-100)); // Already expired
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync(user.Id);

        // Assert
        result.ShouldBeFalse(); // Already expired, not "expiring soon"
    }

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnTrue_When_ExpiringToday()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-90)); // Expiring today (0 days)
        SetupUserManager(user);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync(user.Id);

        // Assert
        result.ShouldBeTrue(); // 0 days is within warning period
    }

    [Fact]
    public async Task IsPasswordExpiringWithinWarningPeriodAsync_Should_ReturnFalse_When_UserNotFound()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((FshUser?)null);

        // Act
        var result = await service.IsPasswordExpiringWithinWarningPeriodAsync("nonexistent-user-id");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetPasswordExpiryStatusAsync Tests

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_ReturnExpiredStatus_When_PasswordExpired()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-100));
        SetupUserManager(user);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync(user.Id);

        // Assert
        result.IsExpired.ShouldBeTrue();
        result.IsExpiringWithinWarningPeriod.ShouldBeFalse();
        result.DaysUntilExpiry.ShouldBeLessThan(0);
        result.ExpiryDate.ShouldNotBeNull();
        result.Status.ShouldBe("Expired");
    }

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_ReturnExpiringSoonStatus_When_WithinWarningPeriod()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-80)); // ~10 days until expiry
        SetupUserManager(user);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync(user.Id);

        // Assert
        result.IsExpired.ShouldBeFalse();
        result.IsExpiringWithinWarningPeriod.ShouldBeTrue();
        result.DaysUntilExpiry.ShouldBeInRange(9, 10); // TotalDays truncates
        result.ExpiryDate.ShouldNotBeNull();
        result.Status.ShouldBe("Expiring Soon");
    }

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_ReturnValidStatus_When_PasswordValid()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90,
            PasswordExpiryWarningDays = 14
        };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-30)); // ~60 days until expiry
        SetupUserManager(user);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync(user.Id);

        // Assert
        result.IsExpired.ShouldBeFalse();
        result.IsExpiringWithinWarningPeriod.ShouldBeFalse();
        result.DaysUntilExpiry.ShouldBeInRange(59, 60); // TotalDays truncates
        result.ExpiryDate.ShouldNotBeNull();
        result.Status.ShouldBe("Valid");
    }

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_ReturnNullExpiryDate_When_ExpiryNotEnforced()
    {
        // Arrange
        var options = new PasswordPolicyOptions { EnforcePasswordExpiry = false };
        var service = CreateService(options);
        var user = CreateUser(DateTime.UtcNow.AddDays(-30));
        SetupUserManager(user);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync(user.Id);

        // Assert
        result.IsExpired.ShouldBeFalse();
        result.IsExpiringWithinWarningPeriod.ShouldBeFalse();
        result.DaysUntilExpiry.ShouldBe(int.MaxValue);
        result.ExpiryDate.ShouldBeNull();
        result.Status.ShouldBe("Valid");
    }

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_CalculateCorrectExpiryDate()
    {
        // Arrange
        var lastChange = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        var user = CreateUser(lastChange);
        SetupUserManager(user);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync(user.Id);

        // Assert
        result.ExpiryDate.ShouldBe(lastChange.AddDays(90));
    }

    [Fact]
    public async Task GetPasswordExpiryStatusAsync_Should_ReturnDefaultStatus_When_UserNotFound()
    {
        // Arrange
        var options = new PasswordPolicyOptions
        {
            EnforcePasswordExpiry = true,
            PasswordExpiryDays = 90
        };
        var service = CreateService(options);
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((FshUser?)null);

        // Act
        var result = await service.GetPasswordExpiryStatusAsync("nonexistent-user-id");

        // Assert
        result.IsExpired.ShouldBeFalse();
        result.IsExpiringWithinWarningPeriod.ShouldBeFalse();
        result.DaysUntilExpiry.ShouldBe(int.MaxValue);
        result.ExpiryDate.ShouldBeNull();
    }

    #endregion

    #region UpdateLastPasswordChangeDateAsync Tests

    [Fact]
    public async Task UpdateLastPasswordChangeDateAsync_Should_SetToCurrentUtcTime()
    {
        // Arrange
        var options = new PasswordPolicyOptions();
        var service = CreateService(options);
        var oldDate = DateTime.UtcNow.AddDays(-100);
        var user = CreateUser(oldDate);
        SetupUserManager(user);
        _userManager.UpdateAsync(user).Returns(IdentityResult.Success);

        // Act
        var beforeUpdate = DateTime.UtcNow;
        await service.UpdateLastPasswordChangeDateAsync(user.Id);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        user.LastPasswordChangeDate.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        user.LastPasswordChangeDate.ShouldBeLessThanOrEqualTo(afterUpdate);
        await _userManager.Received(1).UpdateAsync(user);
    }

    [Fact]
    public async Task UpdateLastPasswordChangeDateAsync_Should_DoNothing_When_UserNotFound()
    {
        // Arrange
        var options = new PasswordPolicyOptions();
        var service = CreateService(options);
        _userManager.FindByIdAsync(Arg.Any<string>()).Returns((FshUser?)null);

        // Act
        await service.UpdateLastPasswordChangeDateAsync("nonexistent-user-id");

        // Assert
        await _userManager.DidNotReceive().UpdateAsync(Arg.Any<FshUser>());
    }

    #endregion
}