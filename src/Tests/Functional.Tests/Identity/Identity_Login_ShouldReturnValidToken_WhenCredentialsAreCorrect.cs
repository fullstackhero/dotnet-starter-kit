using System.Net;
using System.Net.Http.Json;
using FSH.Tests.Functional.Infrastructure;
using FSH.Tests.Shared.Infrastructure;
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
        // Assert: Red phase failing test until wired
        Client.DefaultRequestHeaders.Add("tenant", "root");
        var response = await Client.PostAsJsonAsync("/api/v1/identity/token/issue", new { 
            email = "admin@root.com", 
            password = "123Pa$$word!" 
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<FSH.Modules.Identity.Contracts.DTOs.TokenResponse>();
        content.ShouldNotBeNull();
        content.AccessToken.ShouldNotBeNullOrEmpty();
    }
}
