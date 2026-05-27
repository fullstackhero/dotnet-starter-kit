using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Multitenancy;

// Regression for #1245: anonymous, tenant-scoped endpoints bind a required
// `tenant` header. When it's missing, ASP.NET Core throws BadHttpRequestException
// (StatusCode 400) during parameter binding. GlobalExceptionHandler used to let
// that fall through to a generic 500; it must now surface the framework's 400.
[Collection(FshCollectionDefinition.Name)]
public sealed class MissingTenantTests
{
    private readonly FshWebApplicationFactory _factory;

    public MissingTenantTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ForgotPassword_Should_Return400_When_TenantHeaderMissing()
    {
        using var client = _factory.CreateClient(); // anonymous, NO tenant header

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/forgot-password",
            new { email = "nobody@example.com" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_Should_Return400_When_TenantHeaderMissing()
    {
        using var client = _factory.CreateClient(); // anonymous, NO tenant header

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/reset-password",
            new { email = "nobody@example.com", token = "x", password = "Test@1234!" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SelfRegister_Should_Return400_When_TenantHeaderMissing()
    {
        using var client = _factory.CreateClient(); // anonymous, NO tenant header
        var uniqueId = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/self-register",
            new
            {
                firstName = "Self",
                lastName = "Reg",
                email = $"self-{uniqueId}@example.com",
                userName = $"selfreg-{uniqueId}",
                password = "Test@1234!",
                confirmPassword = "Test@1234!"
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
