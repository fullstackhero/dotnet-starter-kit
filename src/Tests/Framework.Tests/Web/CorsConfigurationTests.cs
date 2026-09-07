using System.Text.Json;

namespace Framework.Tests.Web;

public sealed class CorsConfigurationTests
{
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Production.json")]
    public void AllowedMethods_Should_IncludePatch_When_RestrictedCorsIsConfigured(string fileName)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "HostConfiguration", fileName);

        // Act
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        JsonElement corsOptions = document.RootElement.GetProperty("CorsOptions");
        string[] allowedMethods = corsOptions
            .GetProperty("AllowedMethods")
            .EnumerateArray()
            .Select(method => method.GetString())
            .OfType<string>()
            .ToArray();

        // Assert
        corsOptions.GetProperty("AllowAll").GetBoolean().ShouldBeFalse();
        allowedMethods.ShouldContain("PATCH");
    }
}
