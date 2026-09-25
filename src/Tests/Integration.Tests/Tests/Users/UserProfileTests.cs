using Integration.Tests.Infrastructure;
using Integration.Tests.Infrastructure.Extensions;
using Integration.Tests.Tests.Sessions;

namespace Integration.Tests.Tests.Users;

/// <summary>
/// Covers the self-service profile surface: UpdateUser (PUT /profile) and
/// SetProfileImage (PUT /profile/image). Both force the target id to the
/// authenticated user, so any signed-in user may edit their own profile.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class UserProfileTests
{
    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    public UserProfileTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    #region UpdateUser (PUT /profile)

    [Fact]
    public async Task UpdateProfile_Should_PersistChanges_When_AuthenticatedUserUpdatesOwnProfile()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "upd-profile");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act
        var response = await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile", new
            {
                firstName = "Updated",
                lastName = "Name",
                phoneNumber = "1234567890"
            });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldBe("Updated");
        dto.LastName.ShouldBe("Name");
        dto.PhoneNumber.ShouldBe("1234567890");
    }

    [Fact]
    public async Task UpdateProfile_Should_IgnoreSuppliedId_When_DifferentFromAuthenticatedUser()
    {
        // Arrange — the endpoint forces request.Id to the caller, so supplying another
        // user's id must NOT update that other user.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var caller = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "upd-self");
        var victim = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "upd-victim");
        using var callerClient = await _auth.CreateAuthenticatedClientAsync(caller.Email, caller.Password);

        // Act — caller tries to update the victim by passing victim's id in the body.
        var response = await callerClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile", new
            {
                id = victim.UserId,
                firstName = "Hijacked"
            });

        // Assert — request succeeds but only the caller's own profile is touched.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var victimRecord = await adminClient.GetAsync(
            $"{TestConstants.IdentityBasePath}/users/{victim.UserId}");
        var victimDto = await victimRecord.DeserializeAsync<UserDto>();
        victimDto.FirstName.ShouldNotBe("Hijacked");
    }

    [Fact]
    public async Task UpdateProfile_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile", new { firstName = "Nope" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_Should_Return400_When_PhoneNumberExceedsMaxLength()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "upd-invalid");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act — phone number max length is 15.
        var response = await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile", new
            {
                phoneNumber = new string('9', 30)
            });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Optimistic concurrency (ETag / If-Match)

    [Fact]
    public async Task GetProfile_Should_ReturnStrongETag_When_ProfileIsRead()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-read");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act
        var response = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");

        // Assert — If-Match mandates strong comparison, so the tag must not be weak.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.ETag!.IsWeak.ShouldBeFalse();
        response.Headers.ETag.Tag.ShouldStartWith("\"");
        response.Headers.ETag.Tag.ShouldEndWith("\"");
    }

    [Fact]
    public async Task UpdateProfile_Should_PersistAndRotateETag_When_IfMatchMatches()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-match");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);
        var etag = await ReadProfileETagAsync(userClient);

        // Act
        var response = await PutProfileAsync(userClient, new { firstName = "Matched" }, etag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var reread = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await reread.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldBe("Matched");

        // The token has to move, or a second save built from the same snapshot would be accepted.
        reread.Headers.ETag!.ToString().ShouldNotBe(etag);
        var replay = await PutProfileAsync(userClient, new { firstName = "Replayed" }, etag);
        replay.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task UpdateProfile_Should_Return412AndKeepConcurrentChange_When_IfMatchIsStale()
    {
        // Arrange — the lost update itself: a caller reads, someone else writes, and the caller's
        // full-representation PUT would otherwise echo every old value back over that write.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-stale");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        var staleETag = await ReadProfileETagAsync(userClient);

        // A concurrent writer lands between that read and the write below.
        var concurrent = await PutProfileAsync(
            userClient,
            new { firstName = "Concurrent", lastName = "Winner", phoneNumber = "5550001111" },
            ifMatch: null);
        concurrent.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — the first caller saves the snapshot it loaded before that write.
        var response = await PutProfileAsync(
            userClient,
            new { firstName = "Stale", lastName = "Loser", phoneNumber = "5559998888" },
            staleETag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldBe("Concurrent");
        dto.LastName.ShouldBe("Winner");
        dto.PhoneNumber.ShouldBe("5550001111");
    }

    [Fact]
    public async Task UpdateProfile_Should_Succeed_When_IfMatchIsAny()
    {
        // Arrange — `*` asks only that the resource exist, so it must not block the update.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-any");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act
        var response = await PutProfileAsync(userClient, new { firstName = "Wildcard" }, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldBe("Wildcard");
    }

    [Fact]
    public async Task UpdateProfile_Should_Return412_When_IfMatchIsWeak()
    {
        // Arrange — a weak validator can never satisfy the strong comparison If-Match requires,
        // even when the tag it carries is the current one.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-weak");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);
        var etag = await ReadProfileETagAsync(userClient);

        // Act
        var response = await PutProfileAsync(userClient, new { firstName = "Weak" }, $"W/{etag}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldNotBe("Weak");
    }

    [Theory]
    [InlineData("not-an-entity-tag")]
    [InlineData("\"unterminated")]
    public async Task UpdateProfile_Should_Return400_When_IfMatchIsMalformed(string ifMatch)
    {
        // Arrange — a malformed header is the client's own bug. 412 would send it into a
        // refetch-and-retry loop it can never win, so the request is rejected as a bad request.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-bad");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        // Act
        var response = await PutProfileAsync(userClient, new { firstName = "Malformed" }, ifMatch);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_Should_Succeed_When_IfMatchListContainsCurrentETag()
    {
        // Arrange — If-Match takes a list; matching any entry is enough.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-list");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);
        var etag = await ReadProfileETagAsync(userClient);

        // Act
        var response = await PutProfileAsync(userClient, new { firstName = "Listed" }, $"\"someone-elses-tag\", {etag}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.FirstName.ShouldBe("Listed");
    }

    [Fact]
    public async Task UpdateProfile_Should_KeepAvatar_When_IfMatchIsStaleAndDeleteCurrentImageRequested()
    {
        // Arrange — a rejected delete-my-avatar request must leave the profile exactly as it was.
        // The precondition runs as the first statement after the user is loaded, ahead of the
        // storage calls and of SetPhoneNumberAsync (which persists on its own), so a 412 cannot
        // leave a half-applied update behind.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-image");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        const string imageUrl = "https://cdn.example.com/avatars/keep-me.png";
        var setImage = await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile/image", new { imageUrl });
        setImage.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var staleETag = await ReadProfileETagAsync(userClient);
        var concurrent = await PutProfileAsync(userClient, new { firstName = "Concurrent" }, ifMatch: null);
        concurrent.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await PutProfileAsync(
            userClient,
            new { firstName = "Stale", deleteCurrentImage = true },
            staleETag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.ImageUrl.ShouldBe(imageUrl);
        dto.FirstName.ShouldBe("Concurrent");
    }

    [Fact]
    public async Task GetProfile_Should_ExposeETagToCrossOriginCallers_When_ProfileIsRead()
    {
        // Arrange — ETag is not a CORS-safelisted response header, so the contract only reaches a
        // front-end if the server also lists it in Access-Control-Expose-Headers. Asserted here
        // rather than left as a comment: the front-end specs mock the header, so nothing else in
        // the suite notices when the server stops sending it.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "etag-cors");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{TestConstants.IdentityBasePath}/profile");
        request.Headers.TryAddWithoutValidation("Origin", "http://localhost:5174");

        // Act
        var response = await userClient.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        response.Headers.TryGetValues("Access-Control-Expose-Headers", out var exposedHeaders).ShouldBeTrue();
        exposedHeaders!
            .SelectMany(value => value.Split(','))
            .Select(value => value.Trim())
            .ShouldContain(value => string.Equals(value, "ETag", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<string> ReadProfileETagAsync(HttpClient client)
    {
        var response = await client.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull();
        return response.Headers.ETag!.ToString();
    }

    private static async Task<HttpResponseMessage> PutProfileAsync(HttpClient client, object body, string? ifMatch)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{TestConstants.IdentityBasePath}/profile")
        {
            Content = JsonContent.Create(body)
        };

        if (ifMatch is not null)
        {
            // Unvalidated on purpose: the malformed-header cases have to reach the server.
            request.Headers.TryAddWithoutValidation("If-Match", ifMatch);
        }

        return await client.SendAsync(request);
    }

    #endregion

    #region SetProfileImage (PUT /profile/image)

    [Fact]
    public async Task SetProfileImage_Should_PersistImageUrl_When_AuthenticatedUserSetsAvatar()
    {
        // Arrange
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "img-set");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);
        const string imageUrl = "https://cdn.example.com/avatars/me.png";

        // Act
        var response = await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile/image", new { imageUrl });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.ImageUrl.ShouldBe(imageUrl);
    }

    [Fact]
    public async Task SetProfileImage_Should_ClearImage_When_NullUrlProvided()
    {
        // Arrange — set then clear.
        using var adminClient = await _auth.CreateRootAdminClientAsync();
        var user = await IdentityUserSeeder.CreateLoginableUserAsync(_factory, adminClient, "img-clear");
        using var userClient = await _auth.CreateAuthenticatedClientAsync(user.Email, user.Password);

        await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile/image",
            new { imageUrl = "https://cdn.example.com/avatars/temp.png" });

        // Act
        var clear = await userClient.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile/image", new { imageUrl = (string?)null });

        // Assert
        clear.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var profile = await userClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        var dto = await profile.DeserializeAsync<UserDto>();
        dto.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task SetProfileImage_Should_Return401_When_NotAuthenticated()
    {
        // Arrange
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await client.PutAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/profile/image",
            new { imageUrl = "https://cdn.example.com/x.png" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
