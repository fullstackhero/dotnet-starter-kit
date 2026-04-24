using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Users;

[Collection(FshCollectionDefinition.Name)]
public sealed class SelfRegistrationTests
{
    private readonly FshWebApplicationFactory _factory;

    public SelfRegistrationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SelfRegister_Should_Return201_When_AnonymousAndPayloadIsValid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/self-register", new
        {
            firstName = "Self",
            lastName = "Reg",
            email = $"self-{uniqueId}@example.com",
            userName = $"selfreg-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.DeserializeAsync<RegisterResult>();
        result.UserId.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SelfRegister_Should_Return400_When_PayloadInvalid()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var response = await client.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/self-register", new
        {
            firstName = "",
            lastName = "",
            email = "not-an-email",
            userName = "",
            password = "weak",
            confirmPassword = "weak"
        });

        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task SelfRegister_Should_RejectDuplicate_When_EmailAlreadyExists()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var payload = new
        {
            firstName = "Dup",
            lastName = "Self",
            email = $"selfdup-{uniqueId}@example.com",
            userName = $"selfdup-{uniqueId}",
            password = "Test@1234!",
            confirmPassword = "Test@1234!"
        };

        var firstResponse = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/self-register", payload);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/self-register",
            payload with { userName = $"selfdup2-{uniqueId}" });

        secondResponse.IsSuccessStatusCode.ShouldBeFalse();
    }
}
