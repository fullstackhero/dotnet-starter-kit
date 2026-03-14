using System.Net.Http.Headers;
using System.Net.Http.Json;
using FSH.Tests.Shared.Infrastructure;
using Xunit;

namespace FSH.Tests.Functional.Infrastructure;

[Collection("Functional")]
public abstract class BaseFunctionalTest : IClassFixture<CustomWebApplicationFactory>
{
    protected HttpClient Client { get; }
    protected CustomWebApplicationFactory Factory { get; }

    protected BaseFunctionalTest(CustomWebApplicationFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected async Task AuthenticateAsync(string email, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/identity/token/issue", new { email, password });
        response.EnsureSuccessStatusCode();
        
        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        if (tokenResponse?.AccessToken != null)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);
        }
    }
    
    public sealed class TokenResponse
    {
        public string? AccessToken { get; set; }
    }
}

[CollectionDefinition("Functional")]
public class FunctionalFixture : ICollectionFixture<CustomWebApplicationFactory> { }

