using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Provisioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.DevSeeding;

/// <summary>
/// Dev-only background service that lights up two demo tenants (`acme`, `globex`) along with
/// representative users, custom roles, group memberships, and shared password. Designed to be
/// idempotent — every step checks before creating, so subsequent restarts are no-ops.
///
/// Activation:
///   - Only registered when <c>IHostEnvironment.IsDevelopment()</c>.
///   - Additionally gated on <c>Seed:Demo == true</c> in configuration so a developer can
///     opt out without code changes.
///
/// Coupling:
///   - Uses <see cref="ITenantService"/> to add the tenants (kicks off provisioning via Hangfire).
///   - Polls <see cref="ITenantProvisioningService"/> until each tenant reports
///     <see cref="TenantProvisioningStatus.Completed"/>, then seeds users in that tenant context.
///   - Sets the Finbuckle <see cref="IMultiTenantContextSetter"/> on a fresh scope so the
///     existing tenant-scoped DbContexts (Identity, Catalog, …) write to the right schema.
/// </summary>
internal sealed class DevDataSeeder : BackgroundService
{
    public const string SharedPassword = "Password123!";

    public static readonly DemoTenant Acme = new(
        Id: "acme",
        Name: "Acme Corp",
        AdminEmail: "admin@acme.com",
        Issuer: "fsh.demo.acme",
        Populated: true);

    public static readonly DemoTenant Globex = new(
        Id: "globex",
        Name: "Globex",
        AdminEmail: "admin@globex.com",
        Issuer: "fsh.demo.globex",
        Populated: false);

    private readonly IServiceProvider _services;
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<DevDataSeeder> _logger;

    public DevDataSeeder(
        IServiceProvider services,
        IHostEnvironment env,
        IConfiguration config,
        ILogger<DevDataSeeder> logger)
    {
        _services = services;
        _env = env;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_env.IsDevelopment())
        {
            return;
        }

        // Default-on in Development unless explicitly disabled.
        if (!_config.GetValue("Seed:Demo", true))
        {
            _logger.LogInformation("[DevDataSeeder] disabled via Seed:Demo=false");
            return;
        }

        // Wait for tenant store + identity migrations + Hangfire to come up.
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken).ConfigureAwait(false);

        try
        {
            await EnsureTenantsAsync(stoppingToken).ConfigureAwait(false);
            await SeedRootSuperAdminAsync(stoppingToken).ConfigureAwait(false);
            await SeedTenantUsersAsync(Acme, stoppingToken).ConfigureAwait(false);
            await SeedTenantUsersAsync(Globex, stoppingToken).ConfigureAwait(false);
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[DevDataSeeder] complete · superadmin@root.com · acme + globex demo users · password '{Password}'", SharedPassword);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[DevDataSeeder] failed");
        }
    }

    private async Task EnsureTenantsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();

        foreach (var demo in new[] { Acme, Globex })
        {
            if (await tenantService.ExistsWithIdAsync(demo.Id, cancellationToken).ConfigureAwait(false))
            {
                continue;
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("[DevDataSeeder] creating demo tenant '{TenantId}'", demo.Id);
            }
            await tenantService.CreateAsync(
                demo.Id,
                demo.Name,
                connectionString: null,
                demo.AdminEmail,
                demo.Issuer,
                cancellationToken).ConfigureAwait(false);
        }

        // Wait for provisioning to finish before we attempt to seed users — both schemas
        // must exist or UserManager.CreateAsync explodes.
        var provisioning = scope.ServiceProvider.GetRequiredService<ITenantProvisioningService>();
        foreach (var demo in new[] { Acme, Globex })
        {
            await WaitForProvisioningAsync(provisioning, demo.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WaitForProvisioningAsync(
        ITenantProvisioningService provisioning,
        string tenantId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var latest = await provisioning.GetLatestAsync(tenantId, cancellationToken).ConfigureAwait(false);
            if (latest is { Status: TenantProvisioningStatus.Completed })
            {
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }
        _logger.LogWarning("[DevDataSeeder] tenant '{TenantId}' did not finish provisioning within 2 minutes; skipping user seed", tenantId);
    }

    private async Task SeedRootSuperAdminAsync(CancellationToken cancellationToken)
    {
        var rootTenant = new AppTenantInfo(
            id: MultitenancyConstants.Root.Id,
            name: MultitenancyConstants.Root.Name,
            connectionString: null,
            adminEmail: MultitenancyConstants.Root.EmailAddress,
            issuer: MultitenancyConstants.Root.Issuer);

        await SeedUsersInTenantAsync(rootTenant, BuildRootUsers(), [], cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedTenantUsersAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null)
        {
            return;
        }

        var users = demo.Id == Acme.Id ? BuildAcmeUsers() : BuildGlobexUsers();
        var customRoles = demo.Id == Acme.Id ? BuildAcmeCustomRoles() : Array.Empty<DemoRole>();
        await SeedUsersInTenantAsync(tenant, users, customRoles, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedUsersInTenantAsync(
        AppTenantInfo tenant,
        IReadOnlyList<DemoUser> users,
        IReadOnlyList<DemoRole> customRoles,
        CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = new PasswordHasher<FshUser>();

        // Custom roles (Manager / Support — Acme only)
        foreach (var demoRole in customRoles)
        {
            var role = await roleManager.FindByNameAsync(demoRole.Name).ConfigureAwait(false);
            if (role is null)
            {
                role = new FshRole(demoRole.Name, demoRole.Description);
                await roleManager.CreateAsync(role).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("[DevDataSeeder] [{Tenant}] created custom role '{Role}'", tenant.Id, demoRole.Name);
                }
            }

            var existingClaims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
            foreach (var permission in demoRole.Permissions)
            {
                if (existingClaims.Any(c => c.Type == ClaimConstants.Permission && c.Value == permission))
                {
                    continue;
                }
                context.RoleClaims.Add(new FshRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = ClaimConstants.Permission,
                    ClaimValue = permission,
                    CreatedBy = "DevDataSeeder",
                    CreatedOn = DateTimeOffset.UtcNow,
                });
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var demoUser in users)
        {
            var existing = await userManager.FindByEmailAsync(demoUser.Email).ConfigureAwait(false);
            if (existing is null)
            {
                var user = new FshUser
                {
                    UserName = demoUser.UserName,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    FirstName = demoUser.FirstName,
                    LastName = demoUser.LastName,
                    IsActive = true,
                    NormalizedEmail = demoUser.Email.ToUpperInvariant(),
                    NormalizedUserName = demoUser.UserName.ToUpperInvariant(),
                };
                user.PasswordHash = hasher.HashPassword(user, SharedPassword);
                var created = await userManager.CreateAsync(user).ConfigureAwait(false);
                if (!created.Succeeded)
                {
                    _logger.LogWarning(
                        "[DevDataSeeder] [{Tenant}] failed to create '{Email}': {Errors}",
                        tenant.Id,
                        demoUser.Email,
                        string.Join("; ", created.Errors.Select(e => e.Description)));
                    continue;
                }
                existing = user;
            }
            else
            {
                // The user pre-existed (most commonly the root super-admin
                // already created by IdentityDbInitializer). Realign its
                // password to SharedPassword so the dev login panel's claim
                // is truthful — see EnsureSharedPasswordAsync.
                await EnsureSharedPasswordAsync(userManager, hasher, existing).ConfigureAwait(false);
            }

            foreach (var role in demoUser.Roles)
            {
                if (!await userManager.IsInRoleAsync(existing, role).ConfigureAwait(false))
                {
                    var roleEntity = await roleManager.FindByNameAsync(role).ConfigureAwait(false);
                    if (roleEntity is null) continue;
                    await userManager.AddToRoleAsync(existing, role).ConfigureAwait(false);
                }
            }
        }

        // The tenant admin (admin@<tenant>.com) is provisioned by
        // IdentityDbInitializer, not this seeder, so it never appears in
        // BuildAcmeUsers / BuildGlobexUsers. The framework hashes it with
        // MultitenancyConstants.DefaultPassword ("123Pa$$word!") which
        // contradicts the dev login panel's "Password123!" claim. Resolve
        // the admin by email and realign the password here so a fresh dev
        // start matches what the panel advertises.
        if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            var admin = await userManager.FindByEmailAsync(tenant.AdminEmail).ConfigureAwait(false);
            if (admin is not null)
            {
                await EnsureSharedPasswordAsync(userManager, hasher, admin).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Idempotently realigns a user's password to <see cref="SharedPassword"/>.
    /// No-ops when the password already matches, so restarts don't churn
    /// the hash on every boot. Bypasses Identity's password-validator
    /// pipeline in favour of a direct hash overwrite — matches the rest
    /// of this seeder's approach and is acceptable because the whole
    /// service is gated on Development + <c>Seed:Demo</c>.
    /// </summary>
    private async Task EnsureSharedPasswordAsync(
        UserManager<FshUser> userManager,
        PasswordHasher<FshUser> hasher,
        FshUser user)
    {
        if (await userManager.CheckPasswordAsync(user, SharedPassword).ConfigureAwait(false))
        {
            return;
        }

        user.PasswordHash = hasher.HashPassword(user, SharedPassword);
        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "[DevDataSeeder] failed to reset password for '{Email}': {Errors}",
                user.Email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[DevDataSeeder] aligned '{Email}' to shared dev password", user.Email);
        }
    }

    // ─── Demo content (mirrors clients/dashboard/src/pages/login.demo-accounts.ts) ───

    internal sealed record DemoTenant(string Id, string Name, string AdminEmail, string Issuer, bool Populated);
    internal sealed record DemoUser(
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyList<string> Roles);
    internal sealed record DemoRole(string Name, string Description, IReadOnlyList<string> Permissions);

    private static IReadOnlyList<DemoUser> BuildRootUsers() =>
    [
        new("superadmin", "superadmin@root.com", "Super", "Admin", [RoleConstants.Admin]),
    ];

    private static IReadOnlyList<DemoUser> BuildAcmeUsers() =>
    [
        new("acme.manager",  "manager@acme.com",  "Maya",   "Lin",      ["Manager"]),
        new("acme.support",  "support@acme.com",  "Sam",    "Rivera",   ["Support"]),
        new("acme.alice",    "alice@acme.com",    "Alice",  "Nguyen",   [RoleConstants.Basic]),
        new("acme.bob",      "bob@acme.com",      "Bob",    "Patel",    [RoleConstants.Basic]),
        new("acme.carol",    "carol@acme.com",    "Carol",  "Smith",    [RoleConstants.Basic]),
        new("acme.dan",      "dan@acme.com",      "Dan",    "Mueller",  [RoleConstants.Basic]),
        new("acme.erin",     "erin@acme.com",     "Erin",   "Okafor",   [RoleConstants.Basic]),
        new("acme.frank",    "frank@acme.com",    "Frank",  "Tanaka",   [RoleConstants.Basic]),
        new("acme.gina",     "gina@acme.com",     "Gina",   "Kowalski", [RoleConstants.Basic]),
        new("acme.henry",    "henry@acme.com",    "Henry",  "Park",     [RoleConstants.Basic]),
    ];

    private static IReadOnlyList<DemoUser> BuildGlobexUsers() =>
    [
        new("globex.dave",   "dave@globex.com",   "Dave",   "Hartwell", [RoleConstants.Basic]),
    ];

    private static IReadOnlyList<DemoRole> BuildAcmeCustomRoles() =>
    [
        new(
            "Manager",
            "Operations manager — full catalog + tickets + read-only users.",
            [
                // Identity
                "Permissions.Users.View",
                "Permissions.Users.Update",
                "Permissions.UserRoles.View",
                "Permissions.Roles.View",
                "Permissions.Sessions.View",
                "Permissions.Sessions.Revoke",
                "Permissions.Groups.View",
                // Catalog
                "Permissions.Brands.View",
                "Permissions.Brands.Create",
                "Permissions.Brands.Update",
                "Permissions.Brands.Delete",
                "Permissions.Categories.View",
                "Permissions.Categories.Create",
                "Permissions.Categories.Update",
                "Permissions.Categories.Delete",
                "Permissions.Products.View",
                "Permissions.Products.Create",
                "Permissions.Products.Update",
                "Permissions.Products.Delete",
                // Tickets
                "Permissions.Tickets.View",
                "Permissions.Tickets.Create",
                "Permissions.Tickets.Update",
                "Permissions.Tickets.Delete",
            ]),

        new(
            "Support",
            "Support agent — full tickets + read-only users.",
            [
                "Permissions.Users.View",
                "Permissions.UserRoles.View",
                "Permissions.Sessions.View",
                "Permissions.Sessions.Revoke",
                "Permissions.Tickets.View",
                "Permissions.Tickets.Create",
                "Permissions.Tickets.Update",
            ]),
    ];
}
