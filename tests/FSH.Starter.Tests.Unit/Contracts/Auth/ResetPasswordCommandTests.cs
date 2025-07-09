using FSH.Framework.Core.Auth.Features.PasswordReset;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ResetPasswordCommandTests
{
    [Fact]
    public void ResetPasswordCommand_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.Equal("10000000146", command.TcKimlikNo);
        Assert.Equal("5551234567", command.UserPhoneNumber);
        Assert.Equal("123456", command.SmsCode);
        Assert.Equal("NewPassword123!", command.NewPassword);
    }

    [Fact]
    public void ResetPasswordCommand_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var command = new ResetPasswordCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.UserPhoneNumber);
        Assert.Equal(string.Empty, command.SmsCode);
        Assert.Equal(string.Empty, command.NewPassword);
    }

    [Fact]
    public void IsValid_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.True(command.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidTckn_ShouldReturnFalse()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "invalid",
            UserPhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidPhoneNumber_ShouldReturnFalse()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "invalid",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidPassword_ShouldReturnFalse()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "weak"
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithInvalidSmsCode_ShouldReturnFalse()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "5551234567",
            SmsCode = "12345", // Too short
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithNonNumericSmsCode_ShouldReturnFalse()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146",
            UserPhoneNumber = "5551234567",
            SmsCode = "12345A", // Contains letter
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void GetTcKimlik_ShouldReturnTcknValueObject()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "10000000146"
        };

        // Act
        var tckn = command.GetTcKimlik();

        // Assert
        Assert.NotNull(tckn);
        Assert.Equal("10000000146", tckn.Value);
    }

    [Fact]
    public void GetPhoneNumber_ShouldReturnPhoneNumberValueObject()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            UserPhoneNumber = "5551234567"
        };

        // Act
        var phone = command.GetPhoneNumber();

        // Assert
        Assert.NotNull(phone);
        Assert.Equal("5551234567", phone.Value);
    }

    [Fact]
    public void GetPassword_ShouldReturnPasswordValueObject()
    {
        // Arrange
        var command = new ResetPasswordCommand
        {
            NewPassword = "NewPassword123!"
        };

        // Act
        var password = command.GetPassword();

        // Assert
        Assert.NotNull(password);
        Assert.Equal("NewPassword123!", password.Value);
    }
}
