namespace FSH.Framework.Shared.Multitenancy;

/// <summary>
/// Short-lived in-process buffer that carries the initial admin password from
/// <c>CreateTenantCommand</c> handling through the background provisioning
/// pipeline, until <c>IdentityDbInitializer.SeedAdminUserAsync</c> consumes it.
///
/// The password is never persisted on <c>AppTenantInfo</c> — that row is
/// read-anywhere within the app process and a tenant admin password belongs
/// nowhere except hashed inside <c>AspNetUsers</c>.
///
/// Lives in <c>Shared</c> so the Identity module (the consumer) and the
/// Multitenancy module (the producer + implementer) can both depend on the
/// abstraction without taking a runtime reference to each other.
/// </summary>
public interface ITenantInitialPasswordBuffer
{
    /// <summary>Buffer a password for a tenant id. Overwrites any prior value.</summary>
    void Store(string tenantId, string password);

    /// <summary>Atomically read-and-remove the buffered password for a tenant id.</summary>
    string? TryConsume(string tenantId);
}
