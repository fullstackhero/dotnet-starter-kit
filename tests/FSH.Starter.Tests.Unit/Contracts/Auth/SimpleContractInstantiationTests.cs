using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class SimpleContractInstantiationTests
{
    [Fact]
    public void ForgotPasswordCommand_SimpleInstantiation_ShouldWork()
    {
        // Act
        var command = new ForgotPasswordCommand();
        var command2 = new ForgotPasswordCommand { TcKimlikNo = "12345678901" };

        // Assert
        Assert.NotNull(command);
        Assert.NotNull(command2);
        Assert.Equal("12345678901", command2.TcKimlikNo);
        Assert.Equal(string.Empty, command.TcKimlikNo);
    }

    [Fact]
    public void ResetPasswordCommand_SimpleInstantiation_ShouldWork()
    {
        // Act
        var command = new ResetPasswordCommand();
        var command2 = new ResetPasswordCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "NewPassword123!"
        };

        // Assert
        Assert.NotNull(command);
        Assert.NotNull(command2);
        Assert.Equal("12345678901", command2.TcKimlikNo);
        Assert.Equal("5551234567", command2.PhoneNumber);
        Assert.Equal("123456", command2.SmsCode);
        Assert.Equal("NewPassword123!", command2.NewPassword);
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
        Assert.Equal(string.Empty, command.SmsCode);
        Assert.Equal(string.Empty, command.NewPassword);
    }

    [Fact]
    public void ValidateTcPhoneCommand_SimpleInstantiation_ShouldWork()
    {
        // Act
        var command = new ValidateTcPhoneCommand();
        var command2 = new ValidateTcPhoneCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567"
        };

        // Assert
        Assert.NotNull(command);
        Assert.NotNull(command2);
        Assert.Equal("12345678901", command2.TcKimlikNo);
        Assert.Equal("5551234567", command2.PhoneNumber);
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
    }

    [Fact]
    public void ForgotPasswordCommand_PropertyAccess_ShouldWork()
    {
        // Arrange & Act
        var command = new ForgotPasswordCommand { TcKimlikNo = "12345678901" };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
    }

    [Fact]
    public void ResetPasswordCommand_PropertyAccess_ShouldWork()
    {
        // Arrange & Act
        var command = new ResetPasswordCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "05551234567",
            SmsCode = "123456",
            NewPassword = "NewPass123!"
        };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("05551234567", command.PhoneNumber);
        Assert.Equal("123456", command.SmsCode);
        Assert.Equal("NewPass123!", command.NewPassword);
    }

    [Fact]
    public void ValidateTcPhoneCommand_PropertyAccess_ShouldWork()
    {
        // Arrange & Act
        var command = new ValidateTcPhoneCommand 
        { 
            TcKimlikNo = "12345678901",
            PhoneNumber = "05551234567"
        };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("05551234567", command.PhoneNumber);
    }
}
