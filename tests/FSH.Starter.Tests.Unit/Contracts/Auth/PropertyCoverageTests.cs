using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class PropertyCoverageTests
{    [Fact]
    public void ForgotPasswordCommand_TcKimlikNo_Property_ShouldGetCorrectly()
    {
        // Arrange & Act - This will call the property getter and init accessor
        var command = new ForgotPasswordCommand { TcKimlikNo = "12345678901" };
        var result = command.TcKimlikNo;
        
        // Assert
        Assert.Equal("12345678901", result);
        Assert.NotNull(command.TcKimlikNo);
    }

    [Fact]
    public void ResetPasswordCommand_TcKimlikNo_Property_ShouldGetCorrectly()
    {
        // Arrange
        var command = new ResetPasswordCommand { TcKimlikNo = "12345678901" };

        // Act
        var result = command.TcKimlikNo;

        // Assert
        Assert.Equal("12345678901", result);
    }

    [Fact]
    public void ResetPasswordCommand_PhoneNumber_Property_ShouldGetCorrectly()
    {
        // Arrange
        var command = new ResetPasswordCommand { PhoneNumber = "05551234567" };

        // Act
        var result = command.PhoneNumber;

        // Assert
        Assert.Equal("05551234567", result);
    }

    [Fact]
    public void ResetPasswordCommand_SmsCode_Property_ShouldGetCorrectly()
    {
        // Arrange
        var command = new ResetPasswordCommand { SmsCode = "123456" };

        // Act
        var result = command.SmsCode;

        // Assert
        Assert.Equal("123456", result);
    }

    [Fact]
    public void ResetPasswordCommand_NewPassword_Property_ShouldGetCorrectly()
    {
        // Arrange
        var command = new ResetPasswordCommand { NewPassword = "NewPass123!" };

        // Act
        var result = command.NewPassword;

        // Assert
        Assert.Equal("NewPass123!", result);
    }
}
