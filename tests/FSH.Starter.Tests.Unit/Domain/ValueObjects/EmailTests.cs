using System;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using Xunit;

namespace FSH.Starter.Tests.Unit.Domain.ValueObjects
{
    public class EmailTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("notanemail")]
        [InlineData("test@.com")]
        [InlineData("test@domain")]
        public void Create_InvalidEmail_ReturnsFailure(string input)
        {
            var result = Email.Create(input);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_ValidEmail_ReturnsSuccess()
        {
            var result = Email.Create("test@example.com");
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("TEST@EXAMPLE.COM", result.Value.Value);
        }
    }
}
