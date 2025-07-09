using FSH.Starter.WebApi.Contracts.Common;
using System;
using System.Collections.Generic;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Common;

public class UserDetailTests
{
    [Fact]
    public void UserDetail_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var userDetail = new UserDetail
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "05551234567",
            Profession = "Engineer",
            IsEmailVerified = true,
            EmailConfirmed = true,
            IsActive = true,
            Roles = new List<string> { "user", "member" }
        };

        // Act & Assert
        Assert.NotEqual(Guid.Empty, userDetail.Id);
        Assert.Equal("testuser", userDetail.Username);
        Assert.Equal("test@example.com", userDetail.Email);
        Assert.Equal("John", userDetail.FirstName);
        Assert.Equal("Doe", userDetail.LastName);
        Assert.Equal("05551234567", userDetail.PhoneNumber);
        Assert.Equal("Engineer", userDetail.Profession);
        Assert.True(userDetail.IsEmailVerified);
        Assert.True(userDetail.EmailConfirmed);
        Assert.True(userDetail.IsActive);
        Assert.Contains("user", userDetail.Roles);
        Assert.Contains("member", userDetail.Roles);
    }

    [Fact]
    public void UserDetail_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var userDetail = new UserDetail();

        // Assert
        Assert.Equal(Guid.Empty, userDetail.Id);
        Assert.Null(userDetail.Username);
        Assert.Null(userDetail.Email);
        Assert.Null(userDetail.FirstName);
        Assert.Null(userDetail.LastName);
        Assert.Null(userDetail.PhoneNumber);
        Assert.Null(userDetail.Profession);
        Assert.False(userDetail.IsEmailVerified);
        Assert.False(userDetail.EmailConfirmed);
        Assert.False(userDetail.IsActive);
        Assert.Empty(userDetail.Roles);
    }
}
