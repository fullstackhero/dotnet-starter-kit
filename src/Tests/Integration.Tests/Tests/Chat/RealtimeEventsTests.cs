using System.Net.Http.Json;
using FSH.Modules.Chat.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Microsoft.AspNetCore.SignalR.Client;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class RealtimeEventsTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private const string HubPath = "/api/v1/realtime/hub";
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public RealtimeEventsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    [Fact]
    public async Task SendingMessage_Should_Fire_ChatMessageCreated_To_Channel_Members()
    {
        var token = await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Realtime"));

        await using var hub = await ConnectAsync(token.AccessToken);
        using var inbox = new EventInbox<MessageDto>(hub, "ChatMessageCreated");

        await SendMessageAsync(client, channelId, "live!");

        var received = await inbox.WaitForFirstAsync(m => m.ChannelId == channelId, EventTimeout);
        received.ShouldNotBeNull("Expected ChatMessageCreated for the new channel");
        received.Body.ShouldBe("live!");
    }

    [Fact]
    public async Task EditingMessage_Should_Fire_ChatMessageEdited()
    {
        var token = await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Edit"));
        var messageId = await SendMessageAsync(client, channelId, "before");

        await using var hub = await ConnectAsync(token.AccessToken);
        using var inbox = new EventInbox<MessageDto>(hub, "ChatMessageEdited");

        using var edit = await client.PutAsJsonAsync($"{ChatBasePath}/messages/{messageId}", new { body = "after" });
        edit.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var received = await inbox.WaitForFirstAsync(m => m.Id == messageId, EventTimeout);
        received.ShouldNotBeNull();
        received.Body.ShouldBe("after");
    }

    [Fact]
    public async Task DeletingMessage_Should_Fire_ChatMessageDeleted()
    {
        var token = await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Del"));
        var messageId = await SendMessageAsync(client, channelId, "delete me");

        await using var hub = await ConnectAsync(token.AccessToken);
        using var inbox = new EventInbox<DeletedPayload>(hub, "ChatMessageDeleted");

        using var del = await client.DeleteAsync($"{ChatBasePath}/messages/{messageId}");
        del.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var received = await inbox.WaitForFirstAsync(p => p.MessageId == messageId, EventTimeout);
        received.ShouldNotBeNull();
        received.ChannelId.ShouldBe(channelId);
    }

    [Fact]
    public async Task MarkChannelRead_Should_Fire_ChatChannelRead_To_Self()
    {
        var token = await _auth.GetRootAdminTokenAsync();
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Read"));
        var messageId = await SendMessageAsync(client, channelId, "ping");

        await using var hub = await ConnectAsync(token.AccessToken);
        using var inbox = new EventInbox<ReadPayload>(hub, "ChatChannelRead");

        using var mark = await client.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/read", new { messageId });
        mark.StatusCode.ShouldBe(System.Net.HttpStatusCode.NoContent);

        var received = await inbox.WaitForFirstAsync(p => p.ChannelId == channelId, EventTimeout);
        received.ShouldNotBeNull();
        received.LastReadMessageId.ShouldBe(messageId);
    }

    [Fact]
    public async Task Hub_Should_Reject_Unauthenticated_Connections()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                "http://localhost" + HubPath,
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    options.WebSocketFactory = (_, _) => throw new NotSupportedException();
                    options.SkipNegotiation = false;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                })
            .Build();
        await using var _ = connection;

        await Should.ThrowAsync<Exception>(async () => await connection.StartAsync());
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"chat-rt-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

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
        var message = await response.DeserializeAsync<MessageDto>();
        return message.Id;
    }

    private async Task<HubConnection> ConnectAsync(string accessToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                $"http://localhost{HubPath}?access_token={Uri.EscapeDataString(accessToken)}",
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                    // TestServer has no WebSocket transport — force long-polling.
                    options.WebSocketFactory = (_, _) => throw new NotSupportedException();
                    options.SkipNegotiation = false;
                    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                    options.Headers["tenant"] = TestConstants.RootTenantId;
                })
            .Build();
        await connection.StartAsync();
        return connection;
    }

    private sealed class EventInbox<T> : IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentQueue<T> _items = new();
        private readonly System.Threading.SemaphoreSlim _signal = new(0);
        private readonly IDisposable _subscription;

        public EventInbox(HubConnection connection, string eventName)
        {
            _subscription = connection.On<T>(eventName, payload =>
            {
                _items.Enqueue(payload);
                _signal.Release();
            });
        }

        public async Task<T?> WaitForFirstAsync(Func<T, bool> predicate, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            // Drain anything that arrived before we started awaiting.
            if (TryFindMatch(predicate, out var prearrived))
            {
                return prearrived;
            }
            try
            {
                while (await _signal.WaitAsync(timeout, cts.Token).ConfigureAwait(false))
                {
                    if (TryFindMatch(predicate, out var hit)) return hit;
                }
            }
            catch (OperationCanceledException)
            {
                // Fall through to return null on timeout.
            }
            return default;
        }

        private bool TryFindMatch(Func<T, bool> predicate, out T match)
        {
            // Drain all queued items into a snapshot list, find the first match, requeue the rest.
            var snapshot = new List<T>();
            while (_items.TryDequeue(out var item)) snapshot.Add(item);

            int matchIndex = snapshot.FindIndex(p => predicate(p));
            if (matchIndex < 0)
            {
                foreach (var item in snapshot) _items.Enqueue(item);
                match = default!;
                return false;
            }
            match = snapshot[matchIndex];
            for (int i = 0; i < snapshot.Count; i++)
            {
                if (i != matchIndex) _items.Enqueue(snapshot[i]);
            }
            return true;
        }

        public void Dispose()
        {
            _subscription.Dispose();
            _signal.Dispose();
        }
    }

    private sealed record DeletedPayload(Guid ChannelId, Guid MessageId);
    private sealed record ReadPayload(Guid ChannelId, Guid LastReadMessageId);
}
