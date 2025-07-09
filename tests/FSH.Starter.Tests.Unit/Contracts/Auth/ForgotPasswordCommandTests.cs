using FSH.Framework.Core.Auth.Features.PasswordReset;
using System;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class ForgotPasswordCommandTests
{
    [Fact]
    public void ForgotPasswordCommand_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var birthDate = DateTime.Today.AddYears(-30);
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "10000000146",
            BirthDate = birthDate
        };

        // Act & Assert
        Assert.Equal("10000000146", command.TcknOrMemberNumber);
        Assert.Equal(birthDate, command.BirthDate);
    }

    [Fact]
    public void ForgotPasswordCommand_DefaultValues_ShouldBeEmpty()
    {
        // Arrange & Act
        var command = new ForgotPasswordCommand();

        // Assert
        Assert.Equal(string.Empty, command.TcknOrMemberNumber);
        Assert.Equal(default(DateTime), command.BirthDate);
    }

    [Fact]
    public void IsValid_WithValidTcknAndBirthDate_ShouldReturnTrue()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "10000000146",
            BirthDate = DateTime.Today.AddYears(-30)
        };

        // Act & Assert
        Assert.True(command.IsValid());
    }

    [Fact]
    public void IsValid_WithValidMemberNumberAndBirthDate_ShouldReturnTrue()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "MEM123456",
            BirthDate = DateTime.Today.AddYears(-30)
        };

        // Act & Assert
        Assert.True(command.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyTcknOrMemberNumber_ShouldReturnFalse()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "",
            BirthDate = DateTime.Today.AddYears(-30)
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithDefaultBirthDate_ShouldReturnFalse()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "10000000146",
            BirthDate = default(DateTime)
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void IsValid_WithEmptyBirthDate_ShouldReturnFalse()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "10000000146",
            BirthDate = DateTime.MinValue
        };

        // Act & Assert
        Assert.False(command.IsValid());
    }

    [Fact]
    public void GetTcKimlik_WithValidTckn_ShouldReturnTcknValueObject()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "10000000146"
        };

        // Act
        var tckn = command.GetTcKimlik();

        // Assert
        Assert.NotNull(tckn);
        Assert.Equal("10000000146", tckn.Value);
    }

    [Fact]
    public void GetTcKimlik_WithMemberNumber_ShouldReturnNull()
    {
        // Arrange
        var command = new ForgotPasswordCommand
        {
            TcknOrMemberNumber = "MEM123456"
        };

        // Act
        var tckn = command.GetTcKimlik();

        // Assert
        Assert.Null(tckn);
    }
}
