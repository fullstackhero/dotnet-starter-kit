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

    // Every non-safelisted header the React clients (and the SignalR client) send must be allowed,
    // or the browser rejects the preflight: tenant on every call, X-FSH-App on login,
    // Idempotency-Key on chat sends, X-Requested-With / X-SignalR-User-Agent on hub negotiate.
    [Theory]
    [InlineData("appsettings.json", "tenant")]
    [InlineData("appsettings.json", "x-fsh-app")]
    [InlineData("appsettings.json", "idempotency-key")]
    [InlineData("appsettings.json", "x-requested-with")]
    [InlineData("appsettings.json", "x-signalr-user-agent")]
    [InlineData("appsettings.Production.json", "tenant")]
    [InlineData("appsettings.Production.json", "x-fsh-app")]
    [InlineData("appsettings.Production.json", "idempotency-key")]
    [InlineData("appsettings.Production.json", "x-requested-with")]
    [InlineData("appsettings.Production.json", "x-signalr-user-agent")]
    public void AllowedHeaders_Should_IncludeClientHeader_When_RestrictedCorsIsConfigured(string fileName, string header)
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "HostConfiguration", fileName);

        // Act
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
        string[] allowedHeaders = document.RootElement
            .GetProperty("CorsOptions")
            .GetProperty("AllowedHeaders")
            .EnumerateArray()
            .Select(h => h.GetString())
            .OfType<string>()
            .ToArray();

        // Assert
        allowedHeaders.ShouldContain(h => string.Equals(h, header, StringComparison.OrdinalIgnoreCase));
    }
}
