using FSH.Modules.Chat.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class ChatMessagesTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly AuthHelper _auth;

    public ChatMessagesTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Persist_And_Return_DTO()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Send"));

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "first message", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var message = await response.DeserializeAsync<MessageDto>();
        message.Body.ShouldBe("first message");
        message.ChannelId.ShouldBe(channelId);
        message.ParentMessageId.ShouldBeNull();
        message.DeletedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task ListChannelMessages_Should_Return_Reverse_Chronological()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("List"));

        var firstId = await SendMessageAsync(client, channelId, "first");
        var secondId = await SendMessageAsync(client, channelId, "second");
        var thirdId = await SendMessageAsync(client, channelId, "third");

        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages");
        var messages = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();

        // Guid v7 monotonic — newest first.
        messages.Count.ShouldBeGreaterThanOrEqualTo(3);
        messages[0].Id.ShouldBe(thirdId);
        messages[1].Id.ShouldBe(secondId);
        messages[2].Id.ShouldBe(firstId);
    }

    [Fact]
    public async Task ListChannelMessages_Should_Page_Backwards_With_Before_Cursor()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Page"));

        var ids = new List<Guid>();
        for (int i = 0; i < 5; i++)
        {
            ids.Add(await SendMessageAsync(client, channelId, $"msg {i}"));
        }

        using var firstPage = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages?pageSize=2");
        var page1 = await firstPage.DeserializeAsync<IReadOnlyList<MessageDto>>();
        page1.Count.ShouldBe(2);
        page1[0].Id.ShouldBe(ids[4]);
        page1[1].Id.ShouldBe(ids[3]);

        using var secondPage = await client.GetAsync(
            $"{ChatBasePath}/channels/{channelId}/messages?pageSize=2&before={ids[3]}");
        var page2 = await secondPage.DeserializeAsync<IReadOnlyList<MessageDto>>();
        page2.Count.ShouldBe(2);
        page2[0].Id.ShouldBe(ids[2]);
        page2[1].Id.ShouldBe(ids[1]);
    }

    [Fact]
    public async Task EditMessage_Should_Update_Body_And_Stamp_EditedAt()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Edit"));
        var messageId = await SendMessageAsync(client, channelId, "before");

        using var edit = await client.PutAsJsonAsync($"{ChatBasePath}/messages/{messageId}", new
        {
            body = "after",
        });
        edit.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var list = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages");
        var messages = await list.DeserializeAsync<IReadOnlyList<MessageDto>>();
        var updated = messages.Single(m => m.Id == messageId);
        updated.Body.ShouldBe("after");
        updated.EditedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteMessage_Should_Tombstone_Body_When_Author()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Del"));
        var messageId = await SendMessageAsync(client, channelId, "secret");

        using var del = await client.DeleteAsync($"{ChatBasePath}/messages/{messageId}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var list = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages");
        var messages = await list.DeserializeAsync<IReadOnlyList<MessageDto>>();
        var deleted = messages.Single(m => m.Id == messageId);
        deleted.Body.ShouldBeNull("Soft-deleted message must have body cleared (tombstone).");
        deleted.DeletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task SendMessage_Should_Create_Thread_Reply_And_Bump_Parent_ReplyCount()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Thread"));
        var parentId = await SendMessageAsync(client, channelId, "parent");

        var replyId = await SendMessageAsync(client, channelId, "reply", parentId);
        replyId.ShouldNotBe(Guid.Empty);

        using var list = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages");
        var topLevel = await list.DeserializeAsync<IReadOnlyList<MessageDto>>();
        var parent = topLevel.Single(m => m.Id == parentId);
        parent.ReplyCount.ShouldBe(1, "ReplyCount must increment after a thread reply.");
        topLevel.ShouldNotContain(m => m.Id == replyId,
            "Thread replies must be excluded from the top-level list.");
    }

    [Fact]
    public async Task MarkChannelRead_Should_Advance_Watermark_And_ZeroOut_UnreadCount()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Read"));
        var msgId = await SendMessageAsync(client, channelId, "ping");

        using var mark = await client.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/read", new
        {
            messageId = msgId,
        });
        mark.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var get = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        var ch = await get.DeserializeAsync<ChannelDto>();
        ch.UnreadCount.ShouldBe(0);
        ch.Members[0].LastReadMessageId.ShouldBe(msgId);
    }

    // ─── exceptions ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_Should_Return400_When_Body_Empty()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Empty"));

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "   ", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SendMessage_Should_Return404_When_Channel_Missing()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{Guid.NewGuid()}/messages",
            new { body = "hi", parentMessageId = (Guid?)null, attachments = Array.Empty<object>() });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SendMessage_Should_Reject_Nested_Threads()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Nested"));
        var parentId = await SendMessageAsync(client, channelId, "parent");
        var firstReply = await SendMessageAsync(client, channelId, "reply-1", parentId);

        using var nested = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body = "nested reply", parentMessageId = (Guid?)firstReply, attachments = Array.Empty<object>() });

        nested.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task EditMessage_Should_Return404_When_Message_Missing()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.PutAsJsonAsync($"{ChatBasePath}/messages/{Guid.NewGuid()}", new
        {
            body = "ghost edit",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMessage_Should_Return404_When_Message_Missing()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.DeleteAsync($"{ChatBasePath}/messages/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkChannelRead_Should_Return404_When_Message_Not_In_Channel()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Mismatch"));

        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/read", new
        {
            messageId = Guid.NewGuid(),
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"chat-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

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

    private static async Task<Guid> SendMessageAsync(HttpClient client, Guid channelId, string body, Guid? parentMessageId = null)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId, attachments = Array.Empty<object>() });
        var message = await response.DeserializeAsync<MessageDto>();
        return message.Id;
    }
}
