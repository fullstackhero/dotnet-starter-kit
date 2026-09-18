using System.Text.Json;
using Integration.Middleware.Tests.Infrastructure;

namespace Integration.Middleware.Tests.Tests;

/// <summary>
/// The 401 body is written by JwtBearer's OnChallenge, not by the global exception handler, so it
/// is the one error response that could stay English while every other one is negotiated. This
/// pins it to the request's culture. It also pins the ordering the localization depends on:
/// UseRequestLocalization sits ahead of UseAuthorization, where the challenge is emitted.
/// </summary>
[Collection(MiddlewareCollectionDefinition.Name)]
public sealed class ChallengeLocalizationTests
{
    private readonly MiddlewareWebApplicationFactory _factory;

    public ChallengeLocalizationTests(MiddlewareWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static async Task<(HttpStatusCode Status, string Title, string Detail)> ChallengeAsync(
        HttpClient client,
        string? acceptLanguage)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/identity/profile");
        if (acceptLanguage is not null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        return (
            response.StatusCode,
            root.GetProperty("title").GetString() ?? string.Empty,
            root.GetProperty("detail").GetString() ?? string.Empty);
    }

    [Fact]
    public async Task Challenge_Should_ReturnPortugueseProblemDetails_When_AcceptLanguageIsPtBR()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var (status, title, detail) = await ChallengeAsync(client, "pt-BR");

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
        title.ShouldBe("Não autorizado");
        detail.ShouldBe("É necessário autenticar-se para acessar este recurso.");
    }

    [Fact]
    public async Task Challenge_Should_ReturnEnglishProblemDetails_When_NoAcceptLanguageIsSent()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var (status, title, detail) = await ChallengeAsync(client, acceptLanguage: null);

        // Assert
        status.ShouldBe(HttpStatusCode.Unauthorized);
        title.ShouldBe("Unauthorized");
        detail.ShouldBe("Authentication is required to access this resource.");
    }
}
