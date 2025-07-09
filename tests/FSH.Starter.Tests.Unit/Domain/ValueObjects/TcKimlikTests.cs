using System;
using FSH.Framework.Core.Auth.Domain.ValueObjects;
using Xunit;

namespace FSH.Starter.Tests.Unit.Domain.ValueObjects
{
    public class TcKimlikTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("1234567890")] // 10 hane
        [InlineData("123456789012")] // 12 hane
        [InlineData("abcdefghijk")]
        [InlineData("00000000000")]
        public void Create_InvalidTckn_ReturnsFailure(string input)
        {
            var result = Tckn.Create(input);
            Assert.False(result.IsSuccess);
        }
    }
}
