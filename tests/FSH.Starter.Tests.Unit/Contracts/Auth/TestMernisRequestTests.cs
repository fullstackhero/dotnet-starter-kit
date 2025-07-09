using FSH.Starter.WebApi.Contracts.Auth;
using Xunit;

namespace FSH.Starter.Tests.Unit.Contracts.Auth;

public class TestMernisRequestTests
{
    [Fact]
    public void TestMernisRequest_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var request = new TestMernisRequest
        {
            Tckn = "12345678901",
            FirstName = "John",
            LastName = "Doe",
            BirthYear = 1990
        };

        // Act & Assert
        Assert.Equal("12345678901", request.Tckn);
        Assert.Equal("John", request.FirstName);
        Assert.Equal("Doe", request.LastName);
        Assert.Equal(1990, request.BirthYear);
    }

    [Fact]
    public void TestMernisRequest_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var request = new TestMernisRequest();

        // Assert
        Assert.Null(request.Tckn);
        Assert.Null(request.FirstName);
        Assert.Null(request.LastName);
        Assert.Equal(0, request.BirthYear);
    }
}
