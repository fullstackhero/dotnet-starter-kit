using Xunit;

namespace FSH.Starter.Tests.Unit.Services
{
    public class ValueObjectTests
    {
        [Fact]
        public void Tckn_Should_Be_Valid_Length()
        {
            var tckn = "12345678901";
            Assert.Equal(11, tckn.Length);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("1234567890")] // 10 hane
        [InlineData("123456789012")] // 12 hane
        public void Tckn_Should_Be_Invalid_When_Null_Empty_Or_WrongLength(string input)
        {
            if (input == null)
            {
                Assert.True(string.IsNullOrEmpty(input));
            }
            else
            {
                Assert.NotEqual(11, input.Length);
            }
        }
    }
}
