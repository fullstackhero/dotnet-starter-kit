using FSH.Starter.WebApi.Contracts.Auth;
using System;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ChangePasswordCommandTests
{
    [Fact]
    public void ChangePasswordCommand_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var command = new ChangePasswordCommand
        {
            TcKimlikNo = "12345678901",
            CurrentPassword = "currentPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // Act & Assert
        Assert.Equal("12345678901", command.TcKimlikNo);
        Assert.Equal("currentPassword123!", command.CurrentPassword);
        Assert.Equal("NewPassword123!", command.NewPassword);
        Assert.Equal("NewPassword123!", command.ConfirmNewPassword);
    }

    [Fact]
    public void ChangePasswordCommand_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var command = new ChangePasswordCommand();

        // Assert
        Assert.Null(command.TcKimlikNo);
        Assert.Null(command.CurrentPassword);
        Assert.Null(command.NewPassword);
        Assert.Null(command.ConfirmNewPassword);
    }
}
