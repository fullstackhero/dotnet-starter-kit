using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ForgotPasswordCommandInstantiationTests
{
    [Fact]
    public void ForgotPasswordCommand_Instantiation_ShouldCreateObject()
    {
        // Act
        var command = new ForgotPasswordCommand();

        // Assert
        Assert.NotNull(command);
        Assert.Equal(string.Empty, command.TcKimlikNo);
    }

    [Fact]
    public void ForgotPasswordCommand_PropertyInitialization_ShouldSetValue()
    {
        // Arrange & Act
        var command = new ForgotPasswordCommand { TcKimlikNo = "12345678901" };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
    }
}
