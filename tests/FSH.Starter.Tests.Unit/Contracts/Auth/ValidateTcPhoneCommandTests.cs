using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ValidateTcPhoneCommandTests
{
    [Fact]
    public void ValidateTcPhoneCommand_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var command = new ValidateTcPhoneCommand
        {
            TcKimlikNo = "12345678901",
            PhoneNumber = "05551234567"
        };

        // Act & Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("05551234567", command.PhoneNumber);
    }

    [Fact]
    public void ValidateTcPhoneCommand_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var command = new ValidateTcPhoneCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcKimlikNo);
        Assert.Equal(string.Empty, command.PhoneNumber);
    }
}
