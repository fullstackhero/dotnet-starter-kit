using FSH.Modules.Chat.Contracts.v1.DTOs;
using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;

namespace Integration.Tests.Tests.Chat;

[Collection(FshCollectionDefinition.Name)]
public sealed class ChatChannelsTests
{
    private const string ChatBasePath = "/api/v1/chat";
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public ChatChannelsTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    // ─── happy path ──────────────────────────────────────────────────

    [Fact]
    public async Task CreateChannel_Should_Return200_And_Persist_Channel()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Eng");

        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name,
            description = "Engineering chatter",
            isPrivate = false,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var channelId = await response.DeserializeAsync<Guid>();
        channelId.ShouldNotBe(Guid.Empty);

        using var get = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        var ch = await get.DeserializeAsync<ChannelDto>();
        ch.Name.ShouldBe(name);
        ch.Slug.ShouldNotBeNullOrWhiteSpace();
        ch.Type.ShouldBe(2); // Channel
        ch.Members.ShouldHaveSingleItem();
        ch.UnreadCount.ShouldBe(0);
    }

    [Fact]
    public async Task ListMyChannels_Should_Include_NewlyCreated()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var name = UniqueName("Mine");
        var channelId = await CreateChannelAsync(client, name);

        using var listResponse = await client.GetAsync($"{ChatBasePath}/channels");
        var channels = await listResponse.DeserializeAsync<IReadOnlyList<ChannelDto>>();
        channels.ShouldContain(c => c.Id == channelId);
    }

    [Fact]
    public async Task DiscoverChannels_Should_List_Public_Only_For_Non_Members()
    {
        // Discover excludes channels the caller is already in. The admin auto-joins on create,
        // so they must self-leave the public channel before it surfaces in Discover.
        using var client = await _auth.CreateRootAdminClientAsync();
        var adminUserId = await GetCurrentUserIdAsync(client);
        var publicId = await CreateChannelAsync(client, UniqueName("Pub"), isPrivate: false);
        var privateId = await CreateChannelAsync(client, UniqueName("Priv"), isPrivate: true);

        using var leavePublic = await client.DeleteAsync($"{ChatBasePath}/channels/{publicId}/members/{adminUserId}");
        leavePublic.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var discover = await client.GetAsync($"{ChatBasePath}/channels/discover");
        var channels = await discover.DeserializeAsync<IReadOnlyList<ChannelDto>>();
        channels.ShouldContain(c => c.Id == publicId);
        channels.ShouldNotContain(c => c.Id == privateId);
    }

    [Fact]
    public async Task UpdateChannel_Should_PersistChanges()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Editable"));
        var newName = UniqueName("Renamed");

        using var update = await client.PutAsJsonAsync($"{ChatBasePath}/channels/{channelId}", new
        {
            channelId,
            name = newName,
            description = "Updated description",
            isPrivate = false,
        });
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var get = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        var ch = await get.DeserializeAsync<ChannelDto>();
        ch.Name.ShouldBe(newName);
        ch.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task ArchiveAndRestore_Channel_Should_Roundtrip()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Arch"));

        using var archive = await client.DeleteAsync($"{ChatBasePath}/channels/{channelId}");
        archive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var getAfterArchive = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        getAfterArchive.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        using var restore = await client.PostAsync($"{ChatBasePath}/channels/{channelId}/restore", content: null);
        restore.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var getAfterRestore = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        getAfterRestore.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FindOrCreateDm_Should_BeIdempotent_For_Same_Peer()
    {
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var peerUserId = await RegisterUserAsync(adminClient, "dmpeer");

        using var first = await adminClient.PostAsJsonAsync($"{ChatBasePath}/dms", new
        {
            userIds = new[] { peerUserId },
        });
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstId = await first.DeserializeAsync<Guid>();

        using var second = await adminClient.PostAsJsonAsync($"{ChatBasePath}/dms", new
        {
            userIds = new[] { peerUserId },
        });
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        var secondId = await second.DeserializeAsync<Guid>();

        secondId.ShouldBe(firstId, "Same DM lookup must return the existing channel id (DirectKey uniqueness).");
    }

    [Fact]
    public async Task AddAndRemove_Channel_Member_Should_Update_Membership()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var channelId = await CreateChannelAsync(client, UniqueName("Members"));
        var newMemberId = await RegisterUserAsync(client, "newmember");

        using var add = await client.PostAsJsonAsync($"{ChatBasePath}/channels/{channelId}/members", new
        {
            channelId,
            userIds = new[] { newMemberId },
        });
        add.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var afterAdd = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        var ch1 = await afterAdd.DeserializeAsync<ChannelDto>();
        ch1.Members.ShouldContain(m => m.UserId == newMemberId);

        using var remove = await client.DeleteAsync($"{ChatBasePath}/channels/{channelId}/members/{newMemberId}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var afterRemove = await client.GetAsync($"{ChatBasePath}/channels/{channelId}");
        var ch2 = await afterRemove.DeserializeAsync<ChannelDto>();
        ch2.Members.ShouldNotContain(m => m.UserId == newMemberId);
    }

    // ─── auth gating ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateChannel_Should_Return401_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/channels", new
        {
            name = UniqueName("Anon"),
            description = (string?)null,
            isPrivate = false,
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetChannelById_Should_Return404_When_Channel_Does_Not_Exist()
    {
        using var client = await _auth.CreateRootAdminClientAsync();

        using var response = await client.GetAsync($"{ChatBasePath}/channels/{Guid.NewGuid()}");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FindOrCreateDm_Should_Return400_When_DMing_Self()
    {
        using var client = await _auth.CreateRootAdminClientAsync();
        var rootAdminUserId = await GetCurrentUserIdAsync(client);

        using var response = await client.PostAsJsonAsync($"{ChatBasePath}/dms", new
        {
            userIds = new[] { rootAdminUserId },
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ─── helpers ─────────────────────────────────────────────────────

    private static string UniqueName(string prefix) =>
        $"chat-{prefix}-{Guid.NewGuid().ToString("N")[..8]}";

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

    private static async Task<string> RegisterUserAsync(HttpClient adminClient, string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@example.com";
        var userName = $"{prefix}-{unique}";
        const string password = "Test@1234!";

        var response = await adminClient.PostAsJsonAsync($"{TestConstants.IdentityBasePath}/register", new
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
        return registered.UserId;
    }

    private static async Task<string> GetCurrentUserIdAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var user = await response.DeserializeAsync<UserDto>();
        return user.Id;
    }
}
