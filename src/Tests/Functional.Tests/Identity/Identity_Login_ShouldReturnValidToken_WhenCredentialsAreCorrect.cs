using System.Net;
using System.Net.Http.Json;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Tests.Functional.Infrastructure;
using FSH.Tests.Shared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace FSH.Tests.Functional.Identity;

public class Identity_Login_ShouldReturnValidToken_WhenCredentialsAreCorrect : BaseFunctionalTest
{
    public Identity_Login_ShouldReturnValidToken_WhenCredentialsAreCorrect(CustomWebApplicationFactory factory) 
        : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        var loginRequest = new GenerateTokenCommand("admin@root.com", "123Pa$$word!");
        Client.DefaultRequestHeaders.Add(MultitenancyConstants.Identifier, "root");

        var response = await Client.PostAsJsonAsync("api/v1/identity/token/issue", loginRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
        tokenResponse.ShouldNotBeNull();
        tokenResponse.AccessToken.ShouldNotBeNullOrEmpty();
    }
}
