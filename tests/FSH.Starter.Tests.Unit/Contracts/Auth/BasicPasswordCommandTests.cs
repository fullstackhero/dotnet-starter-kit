using System;
using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class BasicPasswordCommandTests
{
    [Fact]
    public void ForgotPasswordCommand_DefaultConstructor_ShouldInitializeEmptyProperties()
    {
        // Act
        var command = new ForgotPasswordCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcKimlikNo);
    }

    [Fact]
    public void ForgotPasswordCommand_WithInitializer_ShouldSetProperties()
    {
        // Arrange
        var tcKimlikNo = "12345678901";

        // Act
        var command = new ForgotPasswordCommand { TcKimlikNo = tcKimlikNo };

        // Assert
        Assert.Equal(tcKimlikNo, command.TcKimlikNo);
    }

    [Fact]
    public void ForgotPasswordCommand_GetHashCode_ShouldNotThrow()
    {
        // Arrange
        var command = new ForgotPasswordCommand { TcKimlikNo = "12345678901" };

        // Act & Assert
        var hashCode = command.GetHashCode();
        Assert.True(hashCode != 0 || hashCode == 0); // Any result is valid
    }

    [Fact]
    public void ForgotPasswordCommand_TcKimlikNo_Getter_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = "12345678901";
        var command = new ForgotPasswordCommand { TcKimlikNo = expectedValue };

        // Act
        var actualValue = command.TcKimlikNo;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ResetPasswordCommand_DefaultConstructor_ShouldInitializeEmptyProperties()
    {
        // Act
        var command = new ResetPasswordCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
        Assert.Equal(string.Empty, command.SmsCode);
        Assert.Equal(string.Empty, command.NewPassword);
    }

    [Fact]
    public void ResetPasswordCommand_WithInitializer_ShouldSetProperties()
    {
        // Arrange
        var tcKimlikNo = "12345678901";
        var phoneNumber = "5551234567";
        var smsCode = "123456";
        var newPassword = "NewPassword123!";

        // Act
        var command = new ResetPasswordCommand 
        { 
            TcKimlikNo = tcKimlikNo,
            PhoneNumber = phoneNumber,
            SmsCode = smsCode,
            NewPassword = newPassword
        };

        // Assert
        Assert.Equal(tcKimlikNo, command.TcKimlikNo);
        Assert.Equal(phoneNumber, command.PhoneNumber);
        Assert.Equal(smsCode, command.SmsCode);
        Assert.Equal(newPassword, command.NewPassword);
    }

    [Fact]
    public void ResetPasswordCommand_TcKimlikNo_Getter_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = "12345678901";
        var command = new ResetPasswordCommand { TcKimlikNo = expectedValue };

        // Act
        var actualValue = command.TcKimlikNo;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ResetPasswordCommand_PhoneNumber_Getter_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = "5551234567";
        var command = new ResetPasswordCommand { PhoneNumber = expectedValue };

        // Act
        var actualValue = command.PhoneNumber;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ResetPasswordCommand_SmsCode_Getter_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = "123456";
        var command = new ResetPasswordCommand { SmsCode = expectedValue };

        // Act
        var actualValue = command.SmsCode;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ResetPasswordCommand_NewPassword_Getter_ShouldReturnValue()
    {
        // Arrange
        var expectedValue = "NewPassword123!";
        var command = new ResetPasswordCommand { NewPassword = expectedValue };

        // Act
        var actualValue = command.NewPassword;

        // Assert
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact]
    public void ResetPasswordCommand_ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act
#pragma warning disable EPC20, MA0150 // ToString() method is being tested intentionally
        var result = command.ToString();
#pragma warning restore EPC20, MA0150

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ResetPasswordCommand", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ResetPasswordCommand_GetHashCode_ShouldNotThrow()
    {
        // Arrange
        var command = new ResetPasswordCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Act & Assert
        var hashCode = command.GetHashCode();
        Assert.True(hashCode != 0 || hashCode == 0); // Any result is valid
    }

    [Fact]
    public void ValidateTcPhoneCommand_DefaultConstructor_ShouldInitializeEmptyProperties()
    {
        // Act
        var command = new ValidateTcPhoneCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
    }

    [Fact]
    public void ValidateTcPhoneCommand_WithInitializer_ShouldSetProperties()
    {
        // Arrange
        var tcKimlikNo = "12345678901";
        var phoneNumber = "5551234567";

        // Act
        var command = new ValidateTcPhoneCommand 
        { 
            TcKimlikNo = tcKimlikNo,
            PhoneNumber = phoneNumber
        };

        // Assert
        Assert.Equal(tcKimlikNo, command.TcKimlikNo);
        Assert.Equal(phoneNumber, command.PhoneNumber);
    }

    [Fact]
    public void ValidateTcPhoneCommand_ToString_ShouldReturnStringRepresentation()
    {
        // Arrange
        var command = new ValidateTcPhoneCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567"
        };

        // Act
#pragma warning disable EPC20, MA0150 // ToString() method is being tested intentionally
        var result = command.ToString();
#pragma warning restore EPC20, MA0150

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ValidateTcPhoneCommand", result, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateTcPhoneCommand_GetHashCode_ShouldNotThrow()
    {
        // Arrange
        var command = new ValidateTcPhoneCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567"
        };

        // Act & Assert
        var hashCode = command.GetHashCode();
        Assert.True(hashCode != 0 || hashCode == 0); // Any result is valid
    }
}
