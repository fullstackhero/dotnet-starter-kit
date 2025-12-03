namespace FSH.Modules.Multitenancy;

/// <summary>
/// Options controlling multitenancy behavior at startup.
/// </summary>
public sealed class MultitenancyOptions
{
    /// <summary>
    /// When true, runs per-tenant migrations and seeding for all registered <c>IDbInitializer</c>
    /// implementations during <c>UseHeroMultiTenantDatabases</c>. Recommended for development and demos.
    /// In production, prefer running migrations explicitly and leaving this disabled for faster startup.
    /// </summary>
    public bool RunTenantMigrationsOnStartup { get; set; }

    /// <summary>
    /// When true, enqueues tenant provisioning (migrate/seed) jobs on startup for tenants that have not completed provisioning.
    /// Useful to ensure the root tenant is provisioned automatically on first run when startup migrations are disabled.
    /// </summary>
    public bool AutoProvisionOnStartup { get; set; } = true;
}
