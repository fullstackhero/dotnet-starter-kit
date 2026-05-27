using System.Net;
using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class ChatChannelFileAccessTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string FilesBasePath = "/api/v1/files";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChatChannelFileAccessTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task RequestUploadUrl_For_ChatChannel_Should_Succeed_For_Member()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("AttachOk"));

        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "ChatChannel",
            ownerId = channelId,
            fileName = "note.txt",
            contentType = "text/plain",
            sizeBytes = 32,
            visibility = 1, // Private
            category = "Document",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RequestUploadUrl_For_ChatChannel_Should_Return_403_For_NonMember()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(adminClient, Unique("Forbidden"));

        var (bobEmail, bobPassword) = await RegisterAndConfirmAsync(adminClient, "bob");
        using var bobClient = await _auth.CreateAuthenticatedClientAsync(bobEmail, bobPassword);

        using var response = await bobClient.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "ChatChannel",
            ownerId = channelId,
            fileName = "note.txt",
            contentType = "text/plain",
            sizeBytes = 32,
            visibility = 1,
            category = "Document",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RequestUploadUrl_For_ChatChannel_Should_Return_403_When_OwnerId_Missing()
    {
        // Policy requires a channel ownerId — without it CanAttachAsync returns false.
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync($"{FilesBasePath}/upload-url", new
        {
            ownerType = "ChatChannel",
            ownerId = (Guid?)null,
            fileName = "note.txt",
            contentType = "text/plain",
            sizeBytes = 32,
            visibility = 1,
            category = "Document",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string Unique(string prefix) => $"chat-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static async Task<Guid> CreateChannelAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name,
            description = (string?)null,
            isPrivate = false,
        });
        return await response.DeserializeAsync<Guid>();
    }

    private async Task<(string email, string password)> RegisterAndConfirmAsync(HttpClient adminClient, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        var userName = $"{prefix}{unique}";
        const string password = "Test@1234!";

        using var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = prefix,
            lastName = "Test",
            email,
            userName,
            password,
            confirmPassword = password,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await response.DeserializeAsync<RegisterResult>();

        using var scope = _factory.Services.CreateScope();
        var tenant = await scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>()
            .GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByIdAsync(registered.UserId);
        user.ShouldNotBeNull();
        if (!user!.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }

        return (email, password);
    }
}
