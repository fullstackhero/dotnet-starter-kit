namespace Integration.Tests.Infrastructure;

public static class TestConstants
{
    public const string RootTenantId = "root";
    public const string RootAdminEmail = "admin@root.com";
    public const string DefaultPassword = "123Pa$$word!";

    // Documentation IP ranges (RFC 5737) so the trusted-proxy fixture never collides with a real host.
    public const string TrustedProxyIp = "192.0.2.10";
    public const string UntrustedSourceIp = "198.51.100.9";

    public const string JwtIssuer = "fsh.local";
    public const string JwtAudience = "fsh.clients";
    public const string JwtSigningKey = "integration-test-signing-key-that-is-at-least-32-chars-long!!";

    public const string IdentityBasePath = "/api/v1/identity";
    public const string TenantsBasePath = "/api/v1/tenants";
    public const string AuditsBasePath = "/api/v1/audits";
    public const string WebhooksBasePath = "/api/v1/webhooks";
    public const string CatalogBasePath = "/api/v1/catalog";
    public const string TicketsBasePath = "/api/v1";
}
