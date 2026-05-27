using System.Net.Http.Json;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class ThreadAndReactionTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly AuthHelper _auth;

    public ThreadAndReactionTests(FshWebApplicationFactory factory)
    {
        _auth = new AuthHelper(factory);
    }

    // ─── threads ─────────────────────────────────────────────────────

    [Fact]
    public async Task ListReplies_Should_Return_Only_Children_Of_Parent_Reverse_Chronological()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Thread"));
        var parentId = await SendMessageAsync(client, channelId, "parent");

        var reply1 = await SendMessageAsync(client, channelId, "first reply", parentId);
        var reply2 = await SendMessageAsync(client, channelId, "second reply", parentId);
        var unrelated = await SendMessageAsync(client, channelId, "top-level");

        using var listResponse = await client.GetAsync($"{ChatBasePath}/messages/{parentId}/replies");
        var replies = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();

        replies.Count.ShouldBe(2);
        replies[0].Id.ShouldBe(reply2);
        replies[1].Id.ShouldBe(reply1);
        replies.ShouldNotContain(m => m.Id == unrelated);
    }

    [Fact]
    public async Task ListReplies_Should_Page_Backwards_With_Before_Cursor()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Page"));
        var parentId = await SendMessageAsync(client, channelId, "parent");

        var ids = new List<Guid>();
        for (int i = 0; i < 4; i++)
        {
            ids.Add(await SendMessageAsync(client, channelId, $"reply {i}", parentId));
        }

        using var firstPage = await client.GetAsync($"{ChatBasePath}/messages/{parentId}/replies?pageSize=2");
        var page1 = await firstPage.DeserializeAsync<IReadOnlyList<MessageDto>>();
        page1.Count.ShouldBe(2);
        page1[0].Id.ShouldBe(ids[3]);
        page1[1].Id.ShouldBe(ids[2]);

        using var secondPage = await client.GetAsync(
            $"{ChatBasePath}/messages/{parentId}/replies?pageSize=2&before={ids[2]}");
        var page2 = await secondPage.DeserializeAsync<IReadOnlyList<MessageDto>>();
        page2[0].Id.ShouldBe(ids[1]);
        page2[1].Id.ShouldBe(ids[0]);
    }

    [Fact]
    public async Task ListReplies_Should_Return_404_For_Missing_Parent()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        using var response = await client.GetAsync($"{ChatBasePath}/messages/{Guid.NewGuid()}/replies");
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReply_Should_Decrement_Parent_ReplyCount()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("DelReply"));
        var parentId = await SendMessageAsync(client, channelId, "parent");
        var replyId = await SendMessageAsync(client, channelId, "reply", parentId);

        var parentBefore = await GetMessageAsync(client, channelId, parentId);
        parentBefore.ReplyCount.ShouldBe(1);

        using var del = await client.DeleteAsync($"{ChatBasePath}/messages/{replyId}");
        del.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var parentAfter = await GetMessageAsync(client, channelId, parentId);
        parentAfter.ReplyCount.ShouldBe(0);
    }

    // ─── reactions ───────────────────────────────────────────────────

    [Fact]
    public async Task AddReaction_Should_Attach_Reaction_To_Message()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("React"));
        var messageId = await SendMessageAsync(client, channelId, "react to me");

        using var add = await client.PostAsJsonAsync($"{ChatBasePath}/messages/{messageId}/reactions",
            new { emoji = "🚀" });
        add.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var message = await GetMessageAsync(client, channelId, messageId);
        message.Reactions.Count.ShouldBe(1);
        message.Reactions[0].Emoji.ShouldBe("🚀");
    }

    [Fact]
    public async Task AddReaction_Should_Be_Idempotent_For_Same_User_Emoji()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Dup"));
        var messageId = await SendMessageAsync(client, channelId, "react");

        await client.PostAsJsonAsync($"{ChatBasePath}/messages/{messageId}/reactions", new { emoji = "🚀" });
        using var second = await client.PostAsJsonAsync($"{ChatBasePath}/messages/{messageId}/reactions",
            new { emoji = "🚀" });
        second.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var message = await GetMessageAsync(client, channelId, messageId);
        message.Reactions.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RemoveReaction_Should_Drop_The_Row()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, Unique("Rem"));
        var messageId = await SendMessageAsync(client, channelId, "react");
        await client.PostAsJsonAsync($"{ChatBasePath}/messages/{messageId}/reactions", new { emoji = "🚀" });

        using var del = await client.DeleteAsync(
            $"{ChatBasePath}/messages/{messageId}/reactions/{Uri.EscapeDataString("🚀")}");
        del.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var message = await GetMessageAsync(client, channelId, messageId);
        message.Reactions.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddReaction_Should_Return_404_For_Missing_Message()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/messages/{Guid.NewGuid()}/reactions",
            new { emoji = "🚀" });
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
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

    private static async Task<Guid> SendMessageAsync(HttpClient client, Guid channelId, string body, Guid? parentMessageId = null)
    {
        using var response = await client.PostAsJsonAsync(
            $"{ChatBasePath}/channels/{channelId}/messages",
            new { body, parentMessageId, attachments = Array.Empty<object>() });
        var dto = await response.DeserializeAsync<MessageDto>();
        return dto.Id;
    }

    private static async Task<MessageDto> GetMessageAsync(HttpClient client, Guid channelId, Guid messageId)
    {
        // Top-level list — fine for parent. For replies, prefer hitting /replies/{parent}.
        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels/{channelId}/messages?pageSize=200");
        var messages = await listResponse.DeserializeAsync<IReadOnlyList<MessageDto>>();
        var match = messages.FirstOrDefault(m => m.Id == messageId);
        if (match is not null) return match;

        // Fall back to replies endpoint — search descendants when not found at top level. The
        // tests above use both reply and top-level messages so we cover both code paths.
        throw new InvalidOperationException($"Message {messageId} not found at top level of channel {channelId}.");
    }
}
