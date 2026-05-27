using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class SearchMessagesTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public SearchMessagesTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task Search_Should_Match_BodyTokens_In_Member_Channels()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Search"));
        var token = Unique("kangaroo"); // unique-enough to avoid false positives across the shared DB
        await SendMessageAsync(client, channelId, $"the {token} hops far");
        await SendMessageAsync(client, channelId, "an unrelated message");

        var results = await SearchAsync(client, token);
        results.ShouldContain(m => m.ChannelId == channelId && m.Body != null && m.Body.Contains(token));
        // The unrelated message should not surface — FTS only matches the token.
        results.Count(m => m.ChannelId == channelId).ShouldBe(1);
    }

    [Fact]
    public async Task Search_Should_Honor_ChannelId_Scope()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var noiseChannel = await CreateChannelAsync(client, Unique("Noise"));
        var targetChannel = await CreateChannelAsync(client, Unique("Target"));
        var token = Unique("aardvark");

        await SendMessageAsync(client, noiseChannel, $"the {token} burrows");
        await SendMessageAsync(client, targetChannel, $"a single {token} sighted");

        var results = await SearchAsync(client, token, targetChannel);
        results.ShouldAllBe(m => m.ChannelId == targetChannel);
        results.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Search_Should_Exclude_Channels_The_Caller_Is_Not_A_Member_Of()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var token = Unique("platypus");

        // Admin creates and writes in a channel they own.
        var adminChannel = await CreateChannelAsync(adminClient, Unique("AdminOnly"));
        await SendMessageAsync(adminClient, adminChannel, $"the {token} swims");

        // Register a second user who never joins adminChannel — they should see zero hits.
        var bobEmail = $"bob-{Guid.NewGuid().ToString("N")[..8]}@example.com";
        const string password = "Test@1234!";
        await RegisterAndConfirmAsync(adminClient, "bob", bobEmail, password);
        using var bobClient = await _auth.CreateAuthenticatedClientAsync(bobEmail, password);

        var results = await SearchAsync(bobClient, token);
        results.ShouldNotContain(m => m.ChannelId == adminChannel,
            "non-member must not see search hits leaked from a channel they're not in");
    }

    [Fact]
    public async Task Search_Should_Skip_SoftDeleted_Messages()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Tomb"));
        var token = Unique("narwhal");
        var messageId = await SendMessageAsync(client, channelId, $"the {token} sings");

        using var del = await client.DeleteAsync($"{ChatBasePath}/messages/{messageId}");
        del.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var results = await SearchAsync(client, token);
        results.ShouldNotContain(m => m.Id == messageId,
            "soft-deleted messages must not appear in search results");
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string Unique(string prefix) => $"{prefix}{Guid.NewGuid().ToString("N")[..8]}";

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

    private static async Task<Guid> SendMessageAsync(HttpClient client, Guid channelId, string body)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });
        var dto = await response.DeserializeAsync<MessageDto>();
        return dto.Id;
    }

    private static async Task<IReadOnlyList<MessageDto>> SearchAsync(HttpClient client, string q, Guid? channelId = null)
    {
        var path = $"{ChatBasePath}/search?q={Uri.EscapeDataString(q)}";
        if (channelId is { } id) path += $"&channelId={id}";
        using var response = await client.GetAsync(path);
        return await response.DeserializeAsync<IReadOnlyList<MessageDto>>();
    }

    private async Task RegisterAndConfirmAsync(HttpClient adminClient, string prefix, string email, string password)
    {
        using var register = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
        {
            firstName = prefix,
            lastName = "Test",
            email,
            userName = email.Split('@')[0],
            password,
            confirmPassword = password,
        });
        register.StatusCode.ShouldBe(System.Net.HttpStatusCode.Created);
        var registered = await register.DeserializeAsync<RegisterResult>();

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
    }
}
