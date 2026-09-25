using System.Net.Http.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Integration.Tests.Tests.Chat;

/// <summary>
/// Focused integration tests for POST /api/v1/chat/channels/{id}/messages — the path the
/// dashboard composer hits. Built around the actual request shape the SPA emits
/// (Idempotency-Key header + body + parentMessageId + attachments) so any wire-format drift
/// surfaces here before it shows up as "send failed" in the UI.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ChatSendMessageTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string IdempotencyHeader = "Idempotency-Key";
    private const string ReplayedHeader = "Idempotency-Replayed";

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChatSendMessageTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path / wire-format ────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Succeed_When_Mirroring_Dashboard_Payload()
    {
        // Reproduces the dashboard composer's exact wire shape (POST messages + fresh Idempotency-Key header +
        // body { body, parentMessageId: null, attachments: [] }) so any client/contract drift is caught here.
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Dash"));

        using var request = BuildSendRequest(channelId, "hello from the dashboard", Guid.NewGuid().ToString());
        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.DeserializeAsync<MessageDto>();
        dto.ChannelId.ShouldBe(channelId);
        dto.Body.ShouldBe("hello from the dashboard");
        dto.ParentMessageId.ShouldBeNull();
        dto.Attachments.ShouldBeEmpty();
        response.Headers.Contains(ReplayedHeader).ShouldBeFalse(
            "First call with a fresh key must not be marked as replayed.");
    }

    [Fact]
    public async Task SendMessage_Should_Trim_Body_Whitespace()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Trim"));

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "  surrounded by spaces  ", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        var dto = await response.DeserializeAsync<MessageDto>();
        dto.Body.ShouldBe("surrounded by spaces", "Domain Message.Create trims body before persisting.");
    }

    // ─── idempotency ─────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Replay_Same_Response_When_Idempotency_Key_Reused()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Replay"));
        var key = Guid.NewGuid().ToString();

        using var first = await client.SendAsync(BuildSendRequest(channelId, "ping", key));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstDto = await first.DeserializeAsync<MessageDto>();

        using var second = await client.SendAsync(BuildSendRequest(channelId, "ping", key));
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Contains(ReplayedHeader).ShouldBeTrue(
            "Second call with the same idempotency key must carry the Idempotency-Replayed header.");
        var secondDto = await second.DeserializeAsync<MessageDto>();

        secondDto.Id.ShouldBe(firstDto.Id, "Replay must surface the same MessageId — not a brand-new row.");
        secondDto.Body.ShouldBe(firstDto.Body);
        secondDto.CreatedAtUtc.ShouldBe(firstDto.CreatedAtUtc);

        // The channel listing should show exactly one row from this exchange (no double-write).
        using var list = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages");
        var messages = await list.DeserializeAsync<IReadOnlyList<MessageDto>>();
        messages.Count(m => m.Id == firstDto.Id).ShouldBe(1, "A replay must not produce a duplicate row.");
    }

    [Fact]
    public async Task SendMessage_Should_Persist_Two_Rows_For_Different_Idempotency_Keys()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("DiffKey"));

        using var first = await client.SendAsync(BuildSendRequest(channelId, "msg-a", Guid.NewGuid().ToString()));
        using var second = await client.SendAsync(BuildSendRequest(channelId, "msg-b", Guid.NewGuid().ToString()));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.Headers.Contains(ReplayedHeader).ShouldBeFalse();

        var firstDto = await first.DeserializeAsync<MessageDto>();
        var secondDto = await second.DeserializeAsync<MessageDto>();
        secondDto.Id.ShouldNotBe(firstDto.Id);
    }

    [Fact]
    public async Task SendMessage_Should_Return400_When_Idempotency_Key_Exceeds_MaxLength()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("LongKey"));

        // Default MaxKeyLength is 128; 200 chars is comfortably over.
        var oversize = new string('k', 200);

        using var request = BuildSendRequest(channelId, "hi", oversize);
        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ─── validator edge cases ────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Return400_When_Body_Exceeds_MaxLength()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("BigBody"));

        // Validator caps Body at 32_768; 33_000 chars trips it.
        var huge = new string('x', 33_000);

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = huge, parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_Should_Return400_When_Attachments_Exceed_Cap()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("ManyAtt"));

        var attachments = Enumerable.Range(0, 11).Select(i => new
        {
            fileAssetId = (Guid?)null,
            url = $"https://cdn.example.com/{i}.png",
            contentType = "image/png",
            fileName = $"f-{i}.png",
            sizeBytes = 100L,
        }).ToArray();

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "with attachments", parentMessageId = (Guid?)null, attachments });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_Should_Return400_When_Attachment_Url_Empty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("BadAtt"));

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new
            {
                body = "broken attachment",
                parentMessageId = (Guid?)null,
                attachments = new[]
                {
                    new
                    {
                        fileAssetId = (Guid?)null,
                        url = string.Empty,
                        contentType = "image/png",
                        fileName = "f.png",
                        sizeBytes = 100L,
                    },
                },
            });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_Should_Succeed_When_Parent_Is_Top_Level_Message_In_Same_Channel()
    {
        // Threads are 1-deep: a reply to a top-level parent in the *same* channel lands 200 and bumps ReplyCount (covered elsewhere).
        // This locks the happy thread shape so the negative case that follows is meaningful.
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("OkThread"));
        var parentId = await SendMessageAsync(client, channelId, "parent");

        using var reply = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "child", parentMessageId = (Guid?)parentId, attachments = Array.Empty<object>() });
        reply.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendMessage_Should_Return400_When_Parent_Belongs_To_Different_Channel()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelA = await CreateChannelAsync(client, UniqueName("ChanA"));
        var channelB = await CreateChannelAsync(client, UniqueName("ChanB"));
        var foreignParent = await SendMessageAsync(client, channelA, "in-A");

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelB}/messages",
            new { body = "tries to reply across channels", parentMessageId = (Guid?)foreignParent, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest,
            "Parent in another channel must be rejected (cross-channel thread invariant).");
    }

    [Fact]
    public async Task SendMessage_Should_Return404_When_Parent_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("GhostParent"));

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "reply to nothing", parentMessageId = (Guid?)Guid.NewGuid(), attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── auth + membership ──────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Return401_When_Unauthenticated()
    {
        // No bearer token at all — the endpoint requires Identity, so we expect 401 before any
        // domain code runs. Tenant header alone isn't enough.
        using var anon = _factory.CreateClient();
        anon.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await anon.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{Guid.NewGuid()}/messages",
            new { body = "anonymous", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendMessage_Should_Return404_When_Caller_Is_Not_A_Channel_Member()
    {
        // Non-members get 404 (not 403) by design — channel existence must not leak.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var (outsider, _) = await RegisterUserAsync(adminClient, "outsider");

        // Admin makes a private channel; outsider is never invited.
        var channelId = await CreateChannelAsync(adminClient, UniqueName("Closed"), isPrivate: true);

        using var outsiderClient = await _auth.CreateAuthenticatedClientAsync(outsider.Email, outsider.Password);
        using var response = await outsiderClient.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "i don't belong here", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Non-members must see 404 (not 403) so channel existence isn't leaked.");
    }

    // ─── lifecycle: archived channels ───────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Return404_After_Channel_Is_Archived()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Archived"));

        // Archive (soft-delete) the channel.
        using var archive = await client.DeleteAsync($"{ChatBasePath}/channels/{channelId}");
        archive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "after archive", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound,
            "Soft-delete query filter must hide archived channels from the send path.");
    }

    [Fact]
    public async Task SendMessage_Should_Succeed_Again_After_Channel_Is_Restored()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("RoundTrip"));

        using var archive = await client.DeleteAsync($"{ChatBasePath}/channels/{channelId}");
        archive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var restore = await client.PostAsync($"{ChatBasePath}/channels/{channelId}/restore", content: null);
        restore.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var send = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "back from the dead", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        send.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"chat-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

    private static HttpRequestMessage BuildSendRequest(Guid channelId, string body, string idempotencyKey)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{ChatBasePath}/channels/{channelId}/messages")
        {
            Content = JsonContent.Create(new
            {
                body,
                parentMessageId = (Guid?)null,
                attachments = Array.Empty<object>(),
            }),
        };
        request.Headers.Add(IdempotencyHeader, idempotencyKey);
        return request;
    }

    private static async Task<Guid> CreateChannelAsync(HttpClient client, string name, bool isPrivate = false)
    {
        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name,
            description = (string?)null,
            isPrivate,
        });
        return await response.DeserializeAsync<Guid>();
    }

    private static async Task<Guid> SendMessageAsync(HttpClient client, Guid channelId, string body, Guid? parentMessageId = null)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId, attachments = Array.Empty<object>() });
        var message = await response.DeserializeAsync<MessageDto>();
        return message.Id;
    }

    private async Task<(UserCredentials user, string accessToken)> RegisterUserAsync(HttpClient adminClient, string prefix)
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

        // Bypass email confirmation so the user can sign in (see project_email_confirmation memory).
        await ConfirmEmailAsync(registered.UserId);

        var token = await _auth.GetTokenAsync(email, password);
        return (new UserCredentials(registered.UserId, userName, email, password), token.AccessToken);
    }

    private async Task ConfirmEmailAsync(string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var user = await userManager.FindByIdAsync(userId);
        user.ShouldNotBeNull();
        if (!user!.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            (await userManager.UpdateAsync(user)).Succeeded.ShouldBeTrue();
        }
    }

    private sealed record UserCredentials(string Id, string UserName, string Email, string Password);
}
