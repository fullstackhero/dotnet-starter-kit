// Test-only DTOs are populated by System.Text.Json via reflection — the
// SonarAnalyzer can't see the assignments and warns about "unused" private
// setters. Suppressing the noise file-wide rather than annotating each DTO.
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Domain;
using Integration.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests.Tests.Impersonation;

/// <summary>
/// End-to-end coverage of the impersonation flow: start, end, JWT revocation hook,
/// per-grant persistence, cross-tenant rules, and the duration cap.
/// One non-root tenant is provisioned per test class via IAsyncLifetime so tests
/// can exercise both intra-tenant (tenant admin impersonating their own user) and
/// cross-tenant (root operator impersonating into another tenant) paths.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ImpersonationTests : IAsyncLifetime
{
    private const string ImpersonationBasePath = TestConstants.IdentityBasePath + "/impersonation";

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FshWebApplicationFactory _factory;
    private readonly AuthHelper _auth;

    // Populated by InitializeAsync — a freshly provisioned tenant with a known admin user.
    private string _tenantId = default!;
    private string _tenantAdminEmail = default!;
    private string _tenantAdminUserId = default!;
    private string _rootAdminUserId = default!;

    public ImpersonationTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
        _auth = new AuthHelper(factory);
    }

    public async Task InitializeAsync()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        _tenantId = $"imptest-{uniqueId}";
        _tenantAdminEmail = $"admin-{uniqueId}@imptest.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, _tenantId, _tenantAdminEmail);
        await WaitForProvisioningAsync(rootClient, _tenantId);

        // Sign in as the freshly seeded tenant admin to capture their userId from
        // the JWT — using the search endpoint would couple these tests to the
        // (currently buggy) cross-tenant search header override.
        var tenantToken = await GetTokenWithRetryAsync(_tenantAdminEmail, TestConstants.DefaultPassword, _tenantId);
        _tenantAdminUserId = ReadSubject(tenantToken.AccessToken);

        var rootToken = await _auth.GetRootAdminTokenAsync();
        _rootAdminUserId = ReadSubject(rootToken.AccessToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ─── StartImpersonation ─────────────────────────────────────────────

    #region Happy Path

    [Fact]
    public async Task Start_Should_IssueImpersonationToken_When_RootImpersonatesTenantUser()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "verifying tenant onboarding",
            durationMinutes = 15,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ImpersonationResponse>(Json);
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrWhiteSpace();
        body.ActorUserId.ShouldBe(_rootAdminUserId);
        body.ActorTenantId.ShouldBe(TestConstants.RootTenantId);
        body.ImpersonatedUserId.ShouldBe(_tenantAdminUserId);
        body.ImpersonatedTenantId.ShouldBe(_tenantId);
    }

    [Fact]
    public async Task Start_Should_EmbedActorClaims_In_IssuedToken()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var token = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Assert — the impersonation token must carry act_sub/act_tenant so the
        // EndImpersonation handler can swap back to the actor without re-auth.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == "act_sub" && c.Value == _rootAdminUserId);
        jwt.Claims.ShouldContain(c => c.Type == "act_tenant" && c.Value == TestConstants.RootTenantId);
        jwt.Subject.ShouldBe(_tenantAdminUserId);
    }

    [Fact]
    public async Task Start_Should_HonorRequestedDuration_When_WithinCap()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var before = DateTime.UtcNow;

        // Act
        var response = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "duration override check",
            durationMinutes = 10,
        });

        // Assert — expiry should be within a few seconds of `now + 10 min`. The
        // default AccessTokenMinutes is 30 in the test config, so 10 must NOT be
        // coming from the default.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ImpersonationResponse>(Json);
        var expectedMin = before.AddMinutes(10).AddSeconds(-5);
        var expectedMax = before.AddMinutes(10).AddSeconds(60);
        body!.AccessTokenExpiresAt.ShouldBeInRange(expectedMin, expectedMax);
    }

    #endregion

    #region Validation + Authorization

    [Fact]
    public async Task Start_Should_RejectInvalidDuration_When_ExceedsCap()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act — validator caps at 60 min; 999 should bounce with 400.
        var response = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "trying to escape the cap",
            durationMinutes = 999,
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_Should_RejectCrossTenant_When_CallerIsNotRoot()
    {
        // Arrange — tenant admin tries to impersonate into root tenant.
        using var tenantClient = await CreateTenantAdminClientAsync();

        // Act
        var response = await tenantClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _rootAdminUserId,
            targetTenantId = TestConstants.RootTenantId,
            reason = "trying to escalate to root",
        });

        // Assert — server-side check throws ForbiddenException.
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Start_Should_RejectSelfImpersonation()
    {
        // Arrange — root admin tries to impersonate themselves.
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _rootAdminUserId,
            targetTenantId = TestConstants.RootTenantId,
            reason = "pointless self-loop",
        });

        // Assert — handler throws CustomException for this; framework maps to 4xx.
        response.IsSuccessStatusCode.ShouldBeFalse();
        ((int)response.StatusCode).ShouldBeInRange(400, 499);
    }

    [Fact]
    public async Task Start_Should_RejectNestedImpersonation()
    {
        // Arrange — start one impersonation, then try to start another using the
        // impersonation token.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var impersonationToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        using var nestedClient = _factory.CreateClient();
        nestedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", impersonationToken);
        nestedClient.DefaultRequestHeaders.Add("tenant", _tenantId);

        // Act
        var response = await nestedClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "nested attempt",
        });

        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse();
        ((int)response.StatusCode).ShouldBeInRange(400, 499);
    }

    [Fact]
    public async Task Start_Should_Return404_When_TargetUserDoesNotExistInTenant()
    {
        // Arrange — root admin's userId is NOT in the test tenant, so passing it
        // with targetTenantId=<test tenant> must 404. This is the same shape as
        // the bug we hit in the admin app (search returned the wrong tenant's
        // user, then start-impersonation rejected it).
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _rootAdminUserId,
            targetTenantId = _tenantId,
            reason = "wrong-tenant user id",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Start_Should_Return401_When_Anonymous()
    {
        // Arrange
        using var anonClient = _factory.CreateClient();
        anonClient.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        // Act
        var response = await anonClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "anonymous",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    // ─── EndImpersonation ───────────────────────────────────────────────

    #region End

    [Fact]
    public async Task End_Should_IssueActorTokens_When_SessionIsImpersonation()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var impersonationToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        using var endClient = ClientWithBearer(impersonationToken, _tenantId);

        // Act
        var response = await endClient.PostAsync($"{ImpersonationBasePath}/end", content: null);

        // Assert — returns a fresh access+refresh pair for the original actor.
        // On failure dump the problem-detail body so the underlying server
        // exception (DetailedTestExceptionHandler emits it) is visible in the
        // test output instead of a bare status code mismatch.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<TokenResult>(Json);
        body.ShouldNotBeNull();
        body.AccessToken.ShouldNotBeNullOrWhiteSpace();
        body.RefreshToken.ShouldNotBeNullOrWhiteSpace();

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(body.AccessToken);
        jwt.Subject.ShouldBe(_rootAdminUserId);
        // Restored actor token must NOT carry act_sub anymore — otherwise the
        // user would still appear impersonated to permission checks.
        jwt.Claims.ShouldNotContain(c => c.Type == "act_sub");
    }

    [Fact]
    public async Task End_Should_Reject_When_SessionIsNotImpersonation()
    {
        // Arrange — a normal root admin client has no act_sub claim.
        using var rootClient = await _auth.CreateRootAdminClientAsync();

        // Act
        var response = await rootClient.PostAsync($"{ImpersonationBasePath}/end", content: null);

        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse();
        ((int)response.StatusCode).ShouldBeInRange(400, 499);
    }

    #endregion

    // ─── Grant lifecycle + revocation ──────────────────────────────────

    #region Grants

    [Fact]
    public async Task GetGrants_Should_ListActiveGrant_After_Start()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Act
        var response = await rootClient.GetAsync($"{ImpersonationBasePath}/grants?Status=Active");

        // Assert — the just-started grant must be visible to the root operator.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var grants = await response.Content.ReadFromJsonAsync<List<ImpersonationGrantPayload>>(Json);
        grants.ShouldNotBeNull();
        grants.ShouldContain(g =>
            g.ImpersonatedUserId == _tenantAdminUserId
            && g.ImpersonatedTenantId == _tenantId
            && g.ActorUserId == _rootAdminUserId
            && g.Status == "Active");
    }

    [Fact]
    public async Task GetGrants_Should_ScopeByTenant_When_CallerIsTenantAdmin()
    {
        // Arrange — start a cross-tenant grant as root targeting the test tenant.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // The tenant admin lists grants from their own tenant context.
        using var tenantClient = await CreateTenantAdminClientAsync();

        // Act
        var response = await tenantClient.GetAsync($"{ImpersonationBasePath}/grants");

        // Assert — tenant admin should see the grant targeting their tenant
        // (and only grants in their tenant). Verify both presence and scope.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var grants = await response.Content.ReadFromJsonAsync<List<ImpersonationGrantPayload>>(Json);
        grants.ShouldNotBeNull();
        grants.ShouldAllBe(g => g.ImpersonatedTenantId == _tenantId);
        grants.ShouldContain(g => g.ImpersonatedUserId == _tenantAdminUserId);
    }

    [Fact]
    public async Task Revoke_Should_RejectImpersonationToken_OnSubsequentRequest()
    {
        // Arrange — start, identify the grant, revoke.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var impersonationToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        var grants = await rootClient
            .GetFromJsonAsync<List<ImpersonationGrantPayload>>(
                $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var targetGrant = grants!.First(g =>
            g.ImpersonatedUserId == _tenantAdminUserId && g.ImpersonatedTenantId == _tenantId);

        // Act — revoke, then try to use the impersonation token.
        var revokeResponse = await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{targetGrant.Id}/revoke",
            new { reason = "operator left for the day" });
        revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — the JWT validation hook should now 401 the impersonation token.
        // Cache TTL is short (and revoke primes the cache to EndedOrRevoked) so
        // the rejection should be effectively immediate.
        using var killedClient = ClientWithBearer(impersonationToken, _tenantId);
        var meResponse = await killedClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        meResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task End_Should_MarkGrant_As_Ended()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var impersonationToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Act — end via the impersonation session, then list ended grants.
        using var endClient = ClientWithBearer(impersonationToken, _tenantId);
        var endResponse = await endClient.PostAsync($"{ImpersonationBasePath}/end", content: null);
        endResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — grant must show up in the Ended bucket for the root operator.
        var ended = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Ended", Json);
        ended.ShouldNotBeNull();
        ended.ShouldContain(g => g.ImpersonatedUserId == _tenantAdminUserId);
    }

    [Fact]
    public async Task Revoke_Should_BeIdempotent_When_GrantAlreadyTerminal()
    {
        // Arrange — start + revoke once.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var grant = active!.First(g => g.ImpersonatedUserId == _tenantAdminUserId);

        var firstRevoke = await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grant.Id}/revoke",
            new { reason = "first call" });
        firstRevoke.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act — revoke again on the same grant.
        var secondRevoke = await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grant.Id}/revoke",
            new { reason = "second call" });

        // Assert — service treats already-terminal as a no-op and surfaces the
        // existing state; should not 5xx.
        secondRevoke.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_Should_Return404_When_GrantDoesNotExist()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var bogusGuid = Guid.NewGuid();

        // Act
        var response = await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{bogusGuid}/revoke",
            new { reason = "nothing here" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    // ─── Permissions enforcement ────────────────────────────────────────

    #region Permissions

    [Fact]
    public async Task Start_Should_Return403_When_CallerLacksImpersonatePerm()
    {
        // Arrange — a freshly registered non-admin user only carries the Basic
        // role, which does NOT include Users.Impersonate.
        using var tenantAdminClient = await CreateTenantAdminClientAsync();
        var (basicEmail, basicPassword) = await RegisterAndConfirmUserAsync(tenantAdminClient, _tenantId, "basic");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicEmail, basicPassword, _tenantId);

        // Act
        var response = await basicClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason = "basic user trying to impersonate",
        });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetGrants_Should_Return403_When_CallerLacksViewPerm()
    {
        // Arrange
        using var tenantAdminClient = await CreateTenantAdminClientAsync();
        var (basicEmail, basicPassword) = await RegisterAndConfirmUserAsync(tenantAdminClient, _tenantId, "basicview");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicEmail, basicPassword, _tenantId);

        // Act
        var response = await basicClient.GetAsync($"{ImpersonationBasePath}/grants");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Revoke_Should_Return403_When_CallerLacksRevokePerm()
    {
        // Arrange — start a grant as root, identify it, then attempt revoke as
        // a basic user who doesn't hold Impersonation.Revoke.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var grant = active!.First(g => g.ImpersonatedUserId == _tenantAdminUserId);

        using var tenantAdminClient = await CreateTenantAdminClientAsync();
        var (basicEmail, basicPassword) = await RegisterAndConfirmUserAsync(tenantAdminClient, _tenantId, "basicrev");
        using var basicClient = await _auth.CreateAuthenticatedClientAsync(basicEmail, basicPassword, _tenantId);

        // Act
        var response = await basicClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grant.Id}/revoke",
            new { reason = "I shouldn't be able to do this" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    #endregion

    // ─── Cross-tenant authorization on Revoke ──────────────────────────

    #region Cross-tenant revoke

    [Fact]
    public async Task Revoke_Should_Allow_TenantAdmin_When_GrantTargetsTheirTenant()
    {
        // Arrange — root starts an impersonation into the test tenant.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var grant = active!.First(g => g.ImpersonatedUserId == _tenantAdminUserId);

        // Act — the test tenant's admin revokes the grant targeting their tenant.
        using var tenantClient = await CreateTenantAdminClientAsync();
        var response = await tenantClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grant.Id}/revoke",
            new { reason = "we noticed a session targeting our tenant" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Revoke_Should_Return404_When_TenantAdmin_TargetsGrant_OutsideTheirTenant()
    {
        // Arrange — provision a second tenant and create a grant from root into
        // THAT tenant. The first tenant's admin should not be able to see or
        // revoke it.
        var otherTenantId = $"impother-{Guid.NewGuid().ToString("N")[..8]}";
        var otherAdminEmail = $"otheradmin-{Guid.NewGuid().ToString("N")[..8]}@imptest.com";

        using var rootClient = await _auth.CreateRootAdminClientAsync();
        await CreateTenantAsync(rootClient, otherTenantId, otherAdminEmail);
        await WaitForProvisioningAsync(rootClient, otherTenantId);
        var otherToken = await GetTokenWithRetryAsync(otherAdminEmail, TestConstants.DefaultPassword, otherTenantId);
        var otherAdminUserId = ReadSubject(otherToken.AccessToken);

        _ = await StartImpersonationAsync(rootClient, otherAdminUserId, otherTenantId);
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active&ImpersonatedTenantId={otherTenantId}", Json);
        var grant = active!.First(g => g.ImpersonatedTenantId == otherTenantId);

        // Act — the first test tenant's admin tries to revoke this cross-tenant grant.
        using var tenantClient = await CreateTenantAdminClientAsync();
        var response = await tenantClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grant.Id}/revoke",
            new { reason = "fishing" });

        // Assert — handler returns NotFoundException (404) rather than 403 so
        // we don't confirm cross-tenant grant existence to outside callers.
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    // ─── Grant data fidelity ────────────────────────────────────────────

    #region Grant data

    [Fact]
    public async Task Grant_Should_PersistReason_And_ActorIdentity()
    {
        // Arrange
        const string reason = "Customer ticket #4821 — verifying ledger discrepancy";
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var startResponse = await rootClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId = _tenantAdminUserId,
            targetTenantId = _tenantId,
            reason,
            durationMinutes = 15,
        });
        startResponse.EnsureSuccessStatusCode();

        // Act
        var grants = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active&ImpersonatedTenantId={_tenantId}", Json);

        // Assert — reason text round-trips, actor + impersonated identities are
        // captured, and *Name fields are populated from the claims pipeline.
        grants.ShouldNotBeNull();
        var grant = grants.First(g => g.ImpersonatedUserId == _tenantAdminUserId);
        grant.Reason.ShouldBe(reason);
        grant.ActorUserId.ShouldBe(_rootAdminUserId);
        grant.ActorTenantId.ShouldBe(TestConstants.RootTenantId);
        grant.ImpersonatedTenantId.ShouldBe(_tenantId);
        grant.ActorUserName.ShouldNotBeNullOrWhiteSpace();
        grant.ImpersonatedUserName.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Grant_Jti_Should_Match_IssuedJwt()
    {
        // Arrange
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var token = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var jtiClaim = jwt.Claims.First(c => c.Type == "jti").Value;

        // Act — query active grants and find the one with this jti.
        var grants = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);

        // Assert — exactly one grant should match this jti, proving the token's
        // jti is the same value persisted in the grant row (the revocation
        // hook keys off this).
        grants.ShouldNotBeNull();
        grants.Count(g => g.Jti == jtiClaim).ShouldBe(1);
    }

    [Fact]
    public async Task Multiple_Impersonations_Should_HaveUniqueJtis()
    {
        // Arrange — issue two impersonation tokens back to back.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var firstToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var secondToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Act
        var firstJti = new JwtSecurityTokenHandler().ReadJwtToken(firstToken).Claims.First(c => c.Type == "jti").Value;
        var secondJti = new JwtSecurityTokenHandler().ReadJwtToken(secondToken).Claims.First(c => c.Type == "jti").Value;

        // Assert — distinct jtis, distinct grant rows, both Active.
        firstJti.ShouldNotBe(secondJti);
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        active.ShouldNotBeNull();
        active.ShouldContain(g => g.Jti == firstJti);
        active.ShouldContain(g => g.Jti == secondJti);
    }

    [Fact]
    public async Task Revoking_One_Should_NotAffect_Other_Concurrent_Session()
    {
        // Arrange — two active impersonation sessions for the same target.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var keepToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);
        var killToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        var killJti = new JwtSecurityTokenHandler().ReadJwtToken(killToken).Claims.First(c => c.Type == "jti").Value;
        var active = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var killGrant = active!.First(g => g.Jti == killJti);

        // Act — revoke only the second grant.
        var revokeResponse = await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{killGrant.Id}/revoke",
            new { reason = "kill only this one" });
        revokeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert — the OTHER session must still be valid.
        using var keepClient = ClientWithBearer(keepToken, _tenantId);
        var keepProfile = await keepClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        keepProfile.StatusCode.ShouldBe(HttpStatusCode.OK);

        // And the revoked session must be rejected.
        using var killClient = ClientWithBearer(killToken, _tenantId);
        var killProfile = await killClient.GetAsync($"{TestConstants.IdentityBasePath}/profile");
        killProfile.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    // ─── Filter parameters ──────────────────────────────────────────────

    #region Filters

    [Fact]
    public async Task GetGrants_Should_FilterByStatus()
    {
        // Arrange — make sure we have at least one Active and one Revoked grant.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Revoke one to populate the Revoked bucket.
        var seed = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var toRevoke = seed![0];
        await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{toRevoke.Id}/revoke",
            new { reason = "for the filter test" });

        // Start another so Active isn't empty.
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Act
        var activeOnly = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var revokedOnly = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Revoked", Json);

        // Assert — each bucket is internally consistent, and the revoked grant
        // we just made appears under Revoked but not Active.
        activeOnly.ShouldNotBeNull();
        activeOnly.ShouldAllBe(g => g.Status == "Active");
        revokedOnly.ShouldNotBeNull();
        revokedOnly.ShouldAllBe(g => g.Status == "Revoked");
        revokedOnly.ShouldContain(g => g.Id == toRevoke.Id);
        activeOnly.ShouldNotContain(g => g.Id == toRevoke.Id);
    }

    [Fact]
    public async Task GetGrants_Should_FilterByActorUserId()
    {
        // Arrange — root impersonates the tenant admin.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        _ = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        // Act — query for grants by an actor that has none (bogus userId).
        var bogusActorGrants = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?ActorUserId={Guid.NewGuid()}", Json);
        var rootActorGrants = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?ActorUserId={_rootAdminUserId}", Json);

        // Assert — bogus actor returns empty; the root actor query returns at
        // least the grant we just created and only grants by that actor.
        bogusActorGrants.ShouldNotBeNull();
        bogusActorGrants.ShouldBeEmpty();
        rootActorGrants.ShouldNotBeNull();
        rootActorGrants.ShouldNotBeEmpty();
        rootActorGrants.ShouldAllBe(g => g.ActorUserId == _rootAdminUserId);
    }

    #endregion

    // ─── End-after-revoke race ──────────────────────────────────────────

    #region End semantics after revoke

    [Fact]
    public async Task End_Should_Reject_When_GrantWasRevoked_First()
    {
        // Arrange — start, revoke via root, then try to End from the (now-dead)
        // impersonation session.
        using var rootClient = await _auth.CreateRootAdminClientAsync();
        var impersonationToken = await StartImpersonationAsync(rootClient, _tenantAdminUserId, _tenantId);

        var jti = new JwtSecurityTokenHandler().ReadJwtToken(impersonationToken).Claims.First(c => c.Type == "jti").Value;
        var grants = await rootClient.GetFromJsonAsync<List<ImpersonationGrantPayload>>(
            $"{ImpersonationBasePath}/grants?Status=Active", Json);
        var grantId = grants!.First(g => g.Jti == jti).Id;

        await rootClient.PostAsJsonAsync(
            $"{ImpersonationBasePath}/grants/{grantId}/revoke",
            new { reason = "killed before End" });

        using var deadClient = ClientWithBearer(impersonationToken, _tenantId);

        // Act
        var response = await deadClient.PostAsync($"{ImpersonationBasePath}/end", content: null);

        // Assert — the JWT validation hook short-circuits BEFORE the End handler
        // runs, so the caller sees 401, not a successful end with a fresh token.
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    // ─── helpers ────────────────────────────────────────────────────────

    private static async Task<string> StartImpersonationAsync(HttpClient asClient, string targetUserId, string targetTenantId)
    {
        var response = await asClient.PostAsJsonAsync($"{ImpersonationBasePath}/start", new
        {
            targetUserId,
            targetTenantId,
            reason = "test fixture",
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ImpersonationResponse>(Json);
        return body!.AccessToken;
    }

    private HttpClient ClientWithBearer(string accessToken, string tenant)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Add("tenant", tenant);
        return client;
    }

    private async Task<HttpClient> CreateTenantAdminClientAsync()
    {
        return await _auth.CreateAuthenticatedClientAsync(
            _tenantAdminEmail,
            TestConstants.DefaultPassword,
            _tenantId);
    }

    // Token issuance for the freshly seeded tenant admin can race the
    // SeedTenantUserCommand the provisioning pipeline runs — retry briefly.
    private async Task<TokenResult> GetTokenWithRetryAsync(string email, string password, string tenant, int maxRetries = 30)
    {
        Exception? last = null;
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _auth.GetTokenAsync(email, password, tenant);
            }
            catch (HttpRequestException ex)
            {
                last = ex;
                await Task.Delay(500);
            }
        }
        throw last ?? new InvalidOperationException("token issuance failed");
    }

    private static string ReadSubject(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        return jwt.Subject;
    }

    /// <summary>
    /// Register a non-admin user inside the given tenant and force-confirm their
    /// email (the test factory has no SMTP). Returns the credentials so the
    /// caller can sign in. The fresh user only gets the Basic role — useful for
    /// "lacks permission" tests.
    /// </summary>
    private async Task<(string email, string password)> RegisterAndConfirmUserAsync(
        HttpClient adminClient,
        string tenantId,
        string prefix)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"{prefix}-{unique}@imptest.com";
        var userName = $"{prefix}{unique}";
        const string password = "Test@1234!";

        // Endpoint is `/identity/register` (not `/identity/users/register`) —
        // mirrors the existing seed pattern in ChatSendMessageTests.
        using var response = await adminClient.PostAsJsonAsync(
            $"{TestConstants.IdentityBasePath}/register", new
            {
                firstName = prefix,
                lastName = "User",
                email,
                userName,
                password,
                confirmPassword = password,
            });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var registered = await response.Content.ReadFromJsonAsync<RegisterResult>(Json);

        await ConfirmEmailAsync(tenantId, registered!.UserId);
        return (email, password);
    }

    private async Task ConfirmEmailAsync(string tenantId, string userId)
    {
        using var scope = _factory.Services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(tenantId);
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

    private sealed class RegisterResult
    {
        public string UserId { get; set; } = default!;
    }

    private static async Task CreateTenantAsync(HttpClient rootClient, string tenantId, string adminEmail)
    {
        var response = await rootClient.PostAsJsonAsync(TestConstants.TenantsBasePath, new
        {
            id = tenantId,
            name = $"Imp Test {tenantId}",
            connectionString = (string?)null,
            adminEmail,
            issuer = $"{tenantId}.issuer",
        });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    private static async Task WaitForProvisioningAsync(HttpClient client, string tenantId, int maxRetries = 60)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            var statusResponse = await client.GetAsync($"{TestConstants.TenantsBasePath}/{tenantId}/provisioning");
            if (statusResponse.IsSuccessStatusCode)
            {
                var content = await statusResponse.Content.ReadAsStringAsync();
                if (content.Contains("Completed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                if (content.Contains("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Tenant {tenantId} provisioning failed: {content}");
                }
            }
            await Task.Delay(1000);
        }
        throw new TimeoutException($"Tenant {tenantId} did not finish provisioning.");
    }

    // ─── shape mirrors ─────────────────────────────────────────────────

    private sealed class ImpersonationResponse
    {
        public string AccessToken { get; set; } = default!;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string ActorUserId { get; set; } = default!;
        public string ActorTenantId { get; set; } = default!;
        public string ImpersonatedUserId { get; set; } = default!;
        public string ImpersonatedTenantId { get; set; } = default!;
    }

    private sealed class ImpersonationGrantPayload
    {
        public Guid Id { get; set; }
        public string Jti { get; set; } = default!;
        public string ActorUserId { get; set; } = default!;
        public string? ActorUserName { get; set; }
        public string ActorTenantId { get; set; } = default!;
        public string ImpersonatedUserId { get; set; } = default!;
        public string? ImpersonatedUserName { get; set; }
        public string ImpersonatedTenantId { get; set; } = default!;
        public string Reason { get; set; } = default!;
        public string Status { get; set; } = default!;
    }
}
