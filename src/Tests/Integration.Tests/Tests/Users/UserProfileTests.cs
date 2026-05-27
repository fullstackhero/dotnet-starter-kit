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
