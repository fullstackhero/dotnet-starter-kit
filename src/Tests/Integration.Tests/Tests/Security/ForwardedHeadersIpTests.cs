using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Data;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Tests.Security;

/// <summary>
/// Runtime repro for audit finding API-02 (no UseForwardedHeaders → proxy IP collapses the real
/// client IP) plus its security boundary. Token issuance persists a UserSession whose IpAddress comes
/// from RequestContextService.IpAddress => Connection.RemoteIpAddress. The forwarded-headers config
/// trusts only the configured upstream (TestConstants.TrustedProxyIp), so:
/// - a request arriving from the trusted proxy has its X-Forwarded-For honored (real client IP), and
/// - a request arriving from any other source has X-Forwarded-For ignored (spoofing is blocked).
/// TestServer has no socket, so the connection IP is stamped via the X-Test-Remote-Ip header (see
/// TestRemoteIpStartupFilter).
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class ForwardedHeadersIpTests
{
    private const string ForwardedClientIp = "203.0.113.7";

    private readonly FshWebApplicationFactory _factory;

    public ForwardedHeadersIpTests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenIssue_Should_RecordForwardedClientIp_When_RequestArrivesFromTrustedProxy()
    {
        var recordedIp = await IssueTokenAndReadSessionIpAsync(
            connectionIp: TestConstants.TrustedProxyIp,
            forwardedFor: ForwardedClientIp);

        recordedIp.ShouldBe(
            ForwardedClientIp,
            "behind a trusted proxy the persisted session IP should be the real client IP from " +
            "X-Forwarded-For.");
    }

    [Fact]
    public async Task TokenIssue_Should_IgnoreForwardedClientIp_When_RequestArrivesFromUntrustedSource()
    {
        // Both arms send the SAME X-Forwarded-For and differ only in the source address. Asserting the
        // untrusted outcome alone would pass even with forwarded-header processing removed entirely - the
        // connection IP gets persisted either way - so the trusted arm is what makes this a boundary test.
        var fromUntrusted = await IssueTokenAndReadSessionIpAsync(
            connectionIp: TestConstants.UntrustedSourceIp,
            forwardedFor: ForwardedClientIp);

        var fromTrusted = await IssueTokenAndReadSessionIpAsync(
            connectionIp: TestConstants.TrustedProxyIp,
            forwardedFor: ForwardedClientIp);

        fromUntrusted.ShouldBe(
            TestConstants.UntrustedSourceIp,
            "X-Forwarded-For from a source outside the trusted-proxy set must be ignored; the persisted " +
            "IP should be the connection IP, never the attacker-supplied forwarded value.");
        fromUntrusted.ShouldNotBe(ForwardedClientIp);

        fromTrusted.ShouldBe(
            ForwardedClientIp,
            "the identical header from the trusted proxy must be honored - otherwise the assertion above " +
            "passes vacuously, satisfied by forwarded headers never being processed at all.");
    }

    private async Task<string?> IssueTokenAndReadSessionIpAsync(string connectionIp, string forwardedFor)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{TestConstants.IdentityBasePath}/token/issue");
        request.Headers.Add("tenant", TestConstants.RootTenantId);
        request.Headers.Add(TestRemoteIpStartupFilter.RemoteIpHeader, connectionIp);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        request.Content = JsonContent.Create(new
        {
            email = TestConstants.RootAdminEmail,
            password = TestConstants.DefaultPassword,
        });

        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        return await GetNewestSessionIpAsync();
    }

    private async Task<string?> GetNewestSessionIpAsync()
    {
        using var scope = _factory.Services.CreateScope();

        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(TestConstants.RootTenantId);
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(tenant);

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var session = await db.UserSessions
            .AsNoTracking()
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        return session?.IpAddress;
    }
}
