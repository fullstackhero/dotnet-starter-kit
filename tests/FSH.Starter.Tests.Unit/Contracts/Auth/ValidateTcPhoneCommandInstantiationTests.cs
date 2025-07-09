using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ValidateTcPhoneCommandInstantiationTests
{
    [Fact]
    public void ValidateTcPhoneCommand_Instantiation_ShouldCreateObject()
    {
        // Act
        var command = new ValidateTcPhoneCommand();

        // Assert
        Assert.NotNull(command);
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
    }

    [Fact]
    public void ValidateTcPhoneCommand_PropertyInitialization_ShouldSetValues()
    {
        // Arrange & Act
        var command = new ValidateTcPhoneCommand
        {
            TcKimlikNo = "12345678901",
            PhoneNumber = "5551234567"
        };

        // Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("5551234567", command.PhoneNumber);
    }
}
