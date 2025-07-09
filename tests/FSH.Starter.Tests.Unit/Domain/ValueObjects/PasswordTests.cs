using System;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using Xunit;

namespace FSH.Starter.Tests.Unit.Domain.ValueObjects
{
    public class PasswordTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("short")]
        [InlineData("nouppercase1!")]
        [InlineData("NOLOWERCASE1!")]
        [InlineData("NoNumber!")]
        [InlineData("NoSpecial1")]
        public void Create_InvalidPassword_ReturnsFailure(string input)
        {
            var result = Password.Create(input);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_ValidPassword_ReturnsSuccess()
        {
            var result = Password.Create("Valid123!");
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("Valid123!", result.Value!.Value);
        }
    }
}
