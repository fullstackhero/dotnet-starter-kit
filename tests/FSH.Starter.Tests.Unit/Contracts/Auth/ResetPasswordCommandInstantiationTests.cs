using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ResetPasswordCommandInstantiationTests
{
    [Fact]
    public void ResetPasswordCommand_Instantiation_ShouldCreateObject()
    {
        // Act
        var command = new ResetPasswordCommand();

        // Assert
        Assert.NotNull(command);
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
        Assert.Equal(string.Empty, command.SmsCode);
        Assert.Equal(string.Empty, command.NewPassword);
    }

    [Fact]
    public void ResetPasswordCommand_PropertyInitialization_ShouldSetValues()
    {
        // Arrange & Act
        var command = new ResetPasswordCommand
        {
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567",
            SmsCode = "123456",
            NewPassword = "TestPassword123!"
        };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("5551234567", command.PhoneNumber);
        Assert.Equal("123456", command.SmsCode);
        Assert.Equal("TestPassword123!", command.NewPassword);
    }
}
