using System;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using Xunit;

namespace FSH.Starter.Tests.Unit.Domain.ValueObjects
{
    public class PhoneNumberTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("123")]
        [InlineData("6123456789")]
        [InlineData("501234567")]
        [InlineData("50123456789")] // 11 hane
        public void Create_InvalidPhoneNumber_ReturnsFailure(string input)
        {
            var result = PhoneNumber.Create(input);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void Create_ValidPhoneNumber_ReturnsSuccess()
        {
            var result = PhoneNumber.Create("5012345678");
            Assert.True(result.IsSuccess);
            Assert.NotNull(result.Value);
            Assert.Equal("5012345678", result.Value!.Value);
        }
    }
}
