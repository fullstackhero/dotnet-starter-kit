using System.Text.Json;

namespace Integration.Tests.Infrastructure;

public sealed class AuthHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly FshWebApplicationFactory _factory;

    public AuthHelper(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task<TokenResult> GetRootAdminTokenAsync(CancellationToken ct = default)
    {
        return GetTokenAsync(
            TestConstants.RootAdminEmail,
            TestConstants.DefaultPassword,
            TestConstants.RootTenantId,
            ct);
    }

    public async Task<TokenResult> GetTokenAsync(
        string email,
        string password,
        string tenant = "root",
        CancellationToken ct = default)
    {
        using var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", tenant);
        request.Content = JsonContent.Create(new { email, password });

        var response = await client.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return JsonSerializer.Deserialize<TokenResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize token response");
    }

    public async Task<HttpClient> CreateRootAdminClientAsync(CancellationToken ct = default)
    {
        var token = await GetRootAdminTokenAsync(ct).ConfigureAwait(false);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        return client;
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(
        string email,
        string password,
        string tenant = "root",
        CancellationToken ct = default)
    {
        var token = await GetTokenAsync(email, password, tenant, ct).ConfigureAwait(false);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
        client.DefaultRequestHeaders.Add("tenant", tenant);
        return client;
    }
}
