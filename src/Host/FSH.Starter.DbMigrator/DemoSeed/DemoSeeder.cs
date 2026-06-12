using System.Globalization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Data;
using FSH.Modules.Tickets.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Starter.DbMigrator.DemoSeed;

/// <summary>
/// Owns the "rich demo content" that the dev environment needs to feel lived-in:
/// the <c>acme</c> and <c>globex</c> tenants, their demo users, custom roles,
/// catalog content, tickets, and chat. Invoked by the migrator's
/// <c>seed-demo</c> verb — never by the API runtime.
///
/// Idempotent: every step checks before writing, so re-running the verb
/// against an already-seeded database is a no-op.
///
/// Naming: pre-2026-05-17 this lived in the API as <c>DevDataSeeder</c>
/// (a hosted service) — moved here so the API no longer mutates data on
/// startup, matching the same principle that pulled migrations out into
/// this project. See <c>docs/superpowers/specs/2026-05-14-remove-api-auto-migration-design.md</c>.
/// </summary>
internal sealed class DemoSeeder
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<DemoSeeder> _logger;
    private string _sharedPassword = string.Empty;

    public static readonly DemoTenant Acme = new(
        Id: "acme",
        Name: "Acme Corp",
        AdminEmail: "admin@acme.com",
        Issuer: "fsh.demo.acme",
        PlanKey: "pro-annual");

    public static readonly DemoTenant Globex = new(
        Id: "globex",
        Name: "Globex",
        AdminEmail: "admin@globex.com",
        Issuer: "fsh.demo.globex",
        PlanKey: "free");

    public DemoSeeder(IServiceProvider services, IConfiguration config, ILogger<DemoSeeder> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Sourced from configuration so the demo credential isn't hard-coded.
        _sharedPassword = _config["Seed:DemoPassword"]
            ?? throw new InvalidOperationException(
                "Seed:DemoPassword must be configured (see appsettings.Development.json).");

        await EnsureDemoTenantsExistAsync(cancellationToken).ConfigureAwait(false);
        await SeedRootSuperAdminAsync(cancellationToken).ConfigureAwait(false);

        foreach (var demo in new[] { Acme, Globex })
        {
            await SeedTenantSubscriptionAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantUsersAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantCatalogAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantTicketsAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantChatAsync(demo, cancellationToken).ConfigureAwait(false);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] complete · root superadmin + {Acme} + {Globex} populated with users / catalog / tickets / chat",
                Acme.Id, Globex.Id);
        }
    }

    // ─── Tenant provisioning ────────────────────────────────────────────

    /// <summary>
    /// Adds the demo tenants to the catalog if missing, then walks them through
    /// the same <see cref="ITenantService"/> migrate + seed path the runtime
    /// uses. The provisioning service inside the migrator falls back to inline
    /// execution because Hangfire isn't running here — we get a synchronous
    /// "tenant is ready" before this method returns.
    /// </summary>
    private async Task EnsureDemoTenantsExistAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        foreach (var demo in new[] { Acme, Globex })
        {
            var existing = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
            if (existing is null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("[demo-seed] creating tenant '{TenantId}'", demo.Id);
                }
                var tenant = new AppTenantInfo(demo.Id, demo.Name, connectionString: string.Empty, demo.AdminEmail, demo.Issuer);
                tenant.SetValidity(DateTime.UtcNow.AddYears(1));
                await tenantDb.TenantInfo.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
                await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                existing = tenant;
            }

            // Same per-tenant path the migrator's apply verb uses. The Identity initializer creates
            // the tenant admin, while Catalog/Tickets/Chat initializers are no-ops today.
            await tenantService.MigrateTenantAsync(existing, cancellationToken).ConfigureAwait(false);
            await tenantService.SeedTenantAsync(existing, cancellationToken).ConfigureAwait(false);

            await EnsureProvisioningRecordAsync(tenantDb, demo.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Demo tenants are migrated + seeded inline above, bypassing the provisioning
    /// pipeline — so no <see cref="TenantProvisioning"/> row exists and the admin
    /// Provisioning panel would 404. Record a completed run (all steps done) so the
    /// panel shows a real "Completed" history instead. Idempotent: skips if a row
    /// already exists for the tenant.
    /// </summary>
    private static async Task EnsureProvisioningRecordAsync(TenantDbContext tenantDb, string tenantId, CancellationToken cancellationToken)
    {
        var alreadyTracked = await tenantDb.Set<TenantProvisioning>()
            .AnyAsync(p => p.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyTracked)
        {
            return;
        }

        var provisioning = new TenantProvisioning(tenantId, Guid.NewGuid().ToString());
        foreach (var step in Enum.GetValues<TenantProvisioningStepName>())
        {
            var stepEntity = new TenantProvisioningStep(provisioning.Id, step);
            stepEntity.MarkRunning();
            stepEntity.MarkCompleted();
            provisioning.Steps.Add(stepEntity);
        }
        provisioning.MarkCompleted();

        tenantDb.Add(provisioning);
        await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Subscription ──────────────────────────────────────────────────

    /// <summary>
    /// Attaches an active billing <see cref="Subscription"/> to the demo tenant so the dashboard's
    /// PLAN / subscription cards are populated out of the box. The real tenant-create path drives this
    /// via <c>TenantSubscribedIntegrationEvent</c>, but demo tenants are provisioned inline (see
    /// <see cref="EnsureDemoTenantsExistAsync"/>) and never publish it — so we write the row directly.
    ///
    /// Paid plans also get an issued term invoice, matching the real flow. It's written directly
    /// rather than via <c>IBillingService</c> so we don't publish <c>InvoiceIssuedIntegrationEvent</c>
    /// — the one-shot migrator has no outbox dispatcher and demo seeding shouldn't fire
    /// notifications/emails. The subscription's term is aligned to the tenant's <c>ValidUpto</c> so the
    /// dashboard's term matches the enforced validity window.
    ///
    /// Idempotent: skips when the tenant already has an active subscription.
    /// </summary>
    private async Task SeedTenantSubscriptionAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        // BillingDbContext is NOT tenant-filtered (TenantId is an explicit column), so no Finbuckle
        // context juggling is required — we scope by TenantId directly.
        var billingDb = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var plan = await billingDb.Plans
            .FirstOrDefaultAsync(p => p.Key == demo.PlanKey && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "[demo-seed] [{Tenant}] plan '{PlanKey}' not found — skipping subscription", demo.Id, demo.PlanKey);
            }
            return;
        }

        // Reuse the existing active subscription's term if present so re-runs don't re-subscribe but
        // still backfill a missing invoice, otherwise start fresh aligned to the tenant's ValidUpto.
        var existing = await billingDb.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == demo.Id && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        var startUtc = existing?.StartUtc ?? DateTime.UtcNow;
        var endUtc = existing?.EndUtc ?? DateTime.SpecifyKind(tenant.ValidUpto, DateTimeKind.Utc);

        if (existing is null)
        {
            billingDb.Subscriptions.Add(Subscription.Create(demo.Id, plan.Id, startUtc, endUtc));
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "[demo-seed] [{Tenant}] subscribed to plan '{PlanKey}' (term ends {End:o})",
                    demo.Id, plan.Key, endUtc);
            }
        }

        // Paid plans get an issued term invoice (like real CreateTenant), written directly so no InvoiceIssuedIntegrationEvent fires
        // (no outbox dispatcher; seeding mustn't email). Idempotent on invoice number; free plans (term price 0) get none, as in production.
        if (plan.TermPrice > 0m)
        {
            var invoiceNumber = string.Create(
                CultureInfo.InvariantCulture, $"SUB-{startUtc:yyyyMM}-{demo.Id.ToUpperInvariant()}");
            var invoiceExists = await billingDb.Invoices
                .AnyAsync(i => i.TenantId == demo.Id && i.InvoiceNumber == invoiceNumber, cancellationToken)
                .ConfigureAwait(false);
            if (!invoiceExists)
            {
                var invoice = Invoice.CreateDraft(
                    demo.Id, invoiceNumber, startUtc.Year, startUtc.Month, plan.Currency,
                    InvoicePurpose.Subscription, startUtc, endUtc);
                invoice.AddLineItem(
                    InvoiceLineItemKind.BaseFee,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{plan.Name} — {plan.Interval} subscription ({startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd})"),
                    1m,
                    plan.TermPrice);
                invoice.Issue();
                billingDb.Invoices.Add(invoice);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "[demo-seed] [{Tenant}] issued term invoice {InvoiceNumber} ({Amount} {Currency})",
                        demo.Id, invoiceNumber, plan.TermPrice, plan.Currency);
                }
            }
        }

        await billingDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Users + roles ─────────────────────────────────────────────────

    private async Task SeedRootSuperAdminAsync(CancellationToken cancellationToken)
    {
        var rootTenant = new AppTenantInfo(
            id: MultitenancyConstants.Root.Id,
            name: MultitenancyConstants.Root.Name,
            connectionString: string.Empty,
            adminEmail: MultitenancyConstants.Root.EmailAddress,
            issuer: MultitenancyConstants.Root.Issuer);

        await SeedUsersInTenantAsync(rootTenant, BuildRootUsers(), [], cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedTenantUsersAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

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

        foreach (var demoRole in customRoles)
        {
            var role = await roleManager.FindByNameAsync(demoRole.Name).ConfigureAwait(false);
            if (role is null)
            {
                role = new FshRole(demoRole.Name, demoRole.Description);
                await roleManager.CreateAsync(role).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("[demo-seed] [{Tenant}] created custom role '{Role}'", tenant.Id, demoRole.Name);
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
                    CreatedBy = "DemoSeeder",
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
                user.PasswordHash = hasher.HashPassword(user, _sharedPassword);
                var created = await userManager.CreateAsync(user).ConfigureAwait(false);
                if (!created.Succeeded)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(
                            "[demo-seed] [{Tenant}] failed to create '{Email}': {Errors}",
                            tenant.Id, demoUser.Email,
                            string.Join("; ", created.Errors.Select(e => e.Description)));
                    }
                    continue;
                }
                existing = user;
            }
            else
            {
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

        // Tenant admin (admin@<tenant>.com) was created by IdentityDbInitializer with the framework default password.
        // Realign it to the shared password so the dev login panel's advertised credential is truthful.
        if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            var admin = await userManager.FindByEmailAsync(tenant.AdminEmail).ConfigureAwait(false);
            if (admin is not null)
            {
                await EnsureSharedPasswordAsync(userManager, hasher, admin).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureSharedPasswordAsync(
        UserManager<FshUser> userManager,
        PasswordHasher<FshUser> hasher,
        FshUser user)
    {
        if (await userManager.CheckPasswordAsync(user, _sharedPassword).ConfigureAwait(false))
        {
            return;
        }
        user.PasswordHash = hasher.HashPassword(user, _sharedPassword);
        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded && _logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "[demo-seed] failed to reset password for '{Email}': {Errors}",
                user.Email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    // ─── Catalog ────────────────────────────────────────────────────────

    /// <summary>
    /// Idempotently seeds the Catalog demo dataset (4 brands / 11 categories /
    /// 10 products) into the demo tenant. Bails when any catalog row already
    /// exists for that tenant.
    /// </summary>
    private async Task SeedTenantCatalogAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        bool alreadySeeded = await dbContext.Brands.AnyAsync(cancellationToken).ConfigureAwait(false)
            || await dbContext.Categories.AnyAsync(cancellationToken).ConfigureAwait(false)
            || await dbContext.Products.AnyAsync(cancellationToken).ConfigureAwait(false);
        if (alreadySeeded) return;

        var brands = CatalogSeedData.BuildBrands();
        dbContext.Brands.AddRange(brands);

        var (roots, children) = CatalogSeedData.BuildCategories();
        dbContext.Categories.AddRange(roots);
        dbContext.Categories.AddRange(children);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var brandsByName = brands.ToDictionary(b => b.Name, b => b);
        var categoriesByName = roots.Concat(children).ToDictionary(c => c.Name, c => c);
        var products = CatalogSeedData.BuildProducts(brandsByName, categoriesByName);
        dbContext.Products.AddRange(products);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {BrandCount} brands, {CategoryCount} categories, {ProductCount} products",
                tenant.Id, brands.Count, roots.Count + children.Count, products.Count);
        }
    }

    // ─── Tickets ────────────────────────────────────────────────────────

    private async Task SeedTenantTicketsAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<TicketsDbContext>();
        if (await dbContext.Tickets.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var usersByEmail = await userManager.Users
            .ToDictionaryAsync(u => u.Email!, u => Guid.Parse(u.Id), cancellationToken)
            .ConfigureAwait(false);

        Guid? UserId(string email) =>
            usersByEmail.TryGetValue(email, out var id) ? id : null;

        IReadOnlyList<TicketScenario> scenarios;
        if (demo.Id == Acme.Id) scenarios = AcmeTicketScenarios(UserId);
        else if (demo.Id == Globex.Id) scenarios = GlobexTicketScenarios(UserId);
        else scenarios = [];

        int number = 1;
        foreach (var scenario in scenarios)
        {
            if (scenario.ReporterUserId is null) continue;

            var ticket = Ticket.Create(
                number: $"TK-{number.ToString(CultureInfo.InvariantCulture)}",
                title: scenario.Title,
                description: scenario.Description,
                priority: scenario.Priority,
                reporterUserId: scenario.ReporterUserId.Value,
                assignedToUserId: scenario.AssignedToUserId);

            foreach (var (authorUserId, body) in scenario.Comments)
            {
                if (authorUserId is null) continue;
                ticket.AddComment(authorUserId.Value, body);
            }

            if (scenario.Resolve)
            {
                ticket.Resolve(scenario.ResolutionNote);
            }

            dbContext.Tickets.Add(ticket);
            number++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {Count} demo ticket(s)",
                tenant.Id, number - 1);
        }
    }

    // ─── Chat ───────────────────────────────────────────────────────────

    private async Task SeedTenantChatAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        if (await dbContext.Channels.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var usersByEmail = await userManager.Users
            .ToDictionaryAsync(u => u.Email!, u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        string? UserId(string email) =>
            usersByEmail.TryGetValue(email, out var id) ? id : null;

        int channelCount = 0;
        int messageCount = 0;

        if (demo.Id == Acme.Id)
        {
            var general = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@acme.com"),
                name: "general",
                description: "Company-wide announcements and watercooler chatter.",
                isPrivate: false,
                additionalMembers: usersByEmail.Values
                    .Where(id => id != UserId("admin@acme.com"))
                    .ToList(),
                messages:
                [
                    (UserId("admin@acme.com"), "Welcome to Acme! 👋 This channel is for company-wide announcements."),
                    (UserId("manager@acme.com"), "Glad to have everyone here. Standups Mondays 10am sharp."),
                    (UserId("alice@acme.com"), "👋"),
                    (UserId("bob@acme.com"), "Coffee chat in 10?"),
                ],
                cancellationToken);
            if (general is not null) { channelCount++; messageCount += 4; }

            var engineering = await SeedChannelAsync(
                dbContext,
                creator: UserId("manager@acme.com"),
                name: "engineering",
                description: "Eng-only. Tickets, deploys, post-mortems.",
                isPrivate: true,
                additionalMembers: [UserId("alice@acme.com"), UserId("bob@acme.com"), UserId("carol@acme.com")],
                messages:
                [
                    (UserId("manager@acme.com"), "What's everyone shipping this week?"),
                    (UserId("alice@acme.com"), "Login redesign — code review out tomorrow."),
                    (UserId("bob@acme.com"), "Mobile hydration fix, then a perf pass on /reports."),
                ],
                cancellationToken);
            if (engineering is not null) { channelCount++; messageCount += 3; }

            var random = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@acme.com"),
                name: "random",
                description: "Off-topic. Memes, weekend plans, dog photos.",
                isPrivate: false,
                additionalMembers: usersByEmail.Values
                    .Where(id => id != UserId("admin@acme.com"))
                    .ToList(),
                messages:
                [
                    (UserId("gina@acme.com"), "Anyone tried the new ramen place on 5th?"),
                    (UserId("henry@acme.com"), "Two thumbs up. The tonkotsu is worth the wait."),
                ],
                cancellationToken);
            if (random is not null) { channelCount++; messageCount += 2; }

            // DM between alice + bob
            var aliceId = UserId("alice@acme.com");
            var bobId = UserId("bob@acme.com");
            if (aliceId is not null && bobId is not null)
            {
                var dm = ChatChannel.CreateDirect(aliceId, bobId);
                dbContext.Channels.Add(dm);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                dbContext.Messages.Add(Message.Create(dm.Id, aliceId, "hey, got a sec for the hydration thing?"));
                dbContext.Messages.Add(Message.Create(dm.Id, bobId,   "yeah, throw me a repro and i'll look in the morning"));
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                channelCount++; messageCount += 2;
            }
        }
        else if (demo.Id == Globex.Id)
        {
            var general = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@globex.com"),
                name: "general",
                description: "Company-wide channel.",
                isPrivate: false,
                additionalMembers: [UserId("dave@globex.com")],
                messages:
                [
                    (UserId("admin@globex.com"), "Welcome to Globex. Ping me here if you need anything."),
                ],
                cancellationToken);
            if (general is not null) { channelCount++; messageCount += 1; }
        }

        if (_logger.IsEnabled(LogLevel.Information) && channelCount > 0)
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {Channels} chat channel(s) and {Messages} message(s)",
                tenant.Id, channelCount, messageCount);
        }
    }

    private static async Task<ChatChannel?> SeedChannelAsync(
        ChatDbContext dbContext,
        string? creator,
        string name,
        string description,
        bool isPrivate,
        IReadOnlyList<string?> additionalMembers,
        IReadOnlyList<(string? AuthorUserId, string Body)> messages,
        CancellationToken cancellationToken)
    {
        if (creator is null) return null;

        var channel = ChatChannel.CreateChannel(name, description, isPrivate, creator);
        foreach (var memberId in additionalMembers.Where(m => m is not null && m != creator).Distinct())
        {
            try
            {
                channel.AddMember(memberId!, addedByUserId: creator);
            }
            catch (InvalidOperationException)
            {
                // "User already a member" — defensive against duplicate ids in the list.
            }
        }
        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (authorUserId, body) in messages)
        {
            if (authorUserId is null) continue;
            dbContext.Messages.Add(Message.Create(channel.Id, authorUserId, body));
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return channel;
    }

    // ─── Demo content shapes ───────────────────────────────────────────

    internal sealed record DemoTenant(string Id, string Name, string AdminEmail, string Issuer, string PlanKey);
    internal sealed record DemoUser(
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyList<string> Roles);
    internal sealed record DemoRole(string Name, string Description, IReadOnlyList<string> Permissions);
    private sealed record TicketScenario(
        string Title,
        string? Description,
        TicketPriority Priority,
        Guid? ReporterUserId,
        Guid? AssignedToUserId,
        IReadOnlyList<(Guid? AuthorUserId, string Body)> Comments,
        bool Resolve,
        string? ResolutionNote);

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

    // Permission claims reference the module contracts constants — never raw strings.
    // A hand-typed name that doesn't match a registry entry (e.g. the old
    // "Permissions.Brands.View" vs the real "Permissions.Catalog.Brands.View")
    // is a claim that grants nothing, silently.
    private static IReadOnlyList<DemoRole> BuildAcmeCustomRoles() =>
    [
        new(
            "Manager",
            "Operations manager — full catalog + tickets + read-only users.",
            [
                IdentityPermissions.Users.View,
                IdentityPermissions.Users.Update,
                IdentityPermissions.UserRoles.View,
                IdentityPermissions.Roles.View,
                IdentityPermissions.Sessions.View,
                IdentityPermissions.Sessions.Revoke,
                IdentityPermissions.Groups.View,
                CatalogPermissions.Brands.View,
                CatalogPermissions.Brands.Create,
                CatalogPermissions.Brands.Update,
                CatalogPermissions.Brands.Delete,
                CatalogPermissions.Categories.View,
                CatalogPermissions.Categories.Create,
                CatalogPermissions.Categories.Update,
                CatalogPermissions.Categories.Delete,
                CatalogPermissions.Products.View,
                CatalogPermissions.Products.Create,
                CatalogPermissions.Products.Update,
                CatalogPermissions.Products.Delete,
                TicketsPermissions.Tickets.View,
                TicketsPermissions.Tickets.Create,
                TicketsPermissions.Tickets.Update,
                TicketsPermissions.Tickets.Delete,
            ]),

        new(
            "Support",
            "Support agent — full tickets + read-only users.",
            [
                IdentityPermissions.Users.View,
                IdentityPermissions.UserRoles.View,
                IdentityPermissions.Sessions.View,
                IdentityPermissions.Sessions.Revoke,
                TicketsPermissions.Tickets.View,
                TicketsPermissions.Tickets.Create,
                TicketsPermissions.Tickets.Update,
            ]),
    ];

    private static IReadOnlyList<TicketScenario> AcmeTicketScenarios(Func<string, Guid?> uid) =>
    [
        new("Login button broken on mobile",
            "Tapping login on iOS Safari does nothing on first tap. Have to double-tap.",
            TicketPriority.High,
            uid("alice@acme.com"),
            uid("support@acme.com"),
            [
                (uid("support@acme.com"),
                 "Confirmed on iPhone 15 Safari. Looks like a hydration race on the auth button. Looking now."),
            ],
            Resolve: false,
            ResolutionNote: null),

        new("Add dark mode to the dashboard",
            "Several customers have asked. Let's match the system preference by default.",
            TicketPriority.Medium,
            uid("bob@acme.com"),
            uid("manager@acme.com"),
            [],
            Resolve: false,
            ResolutionNote: null),

        new("Slow page load on /reports",
            "Initial render takes 6-8s with the full quarter view. Need to chunk the query or add a loader.",
            TicketPriority.High,
            uid("carol@acme.com"),
            uid("manager@acme.com"),
            [
                (uid("manager@acme.com"), "Profiling now — the join against audits is the culprit."),
                (uid("carol@acme.com"),   "Thanks. Let me know if you need a repro account."),
            ],
            Resolve: false,
            ResolutionNote: null),

        new("Update copyright year in footer",
            "Footer still says © 2024. Tiny fix, just flagging.",
            TicketPriority.Low,
            uid("dan@acme.com"),
            uid("support@acme.com"),
            [],
            Resolve: true,
            ResolutionNote: "Bumped to 2026 and added a year() helper so we don't have to chase it again."),

        new("Email notifications missing tenant logo",
            "Notification emails render the default placeholder instead of the tenant brand mark.",
            TicketPriority.Medium,
            uid("erin@acme.com"),
            uid("manager@acme.com"),
            [
                (uid("manager@acme.com"),
                 "Template was hardcoded to /assets/default.png — switched to tenant.theme.logoUrl."),
            ],
            Resolve: true,
            ResolutionNote: "Released in 1.4.2. Verified across acme and globex."),

        new("Onboarding survey wording feels stiff",
            "Step 3 copy is robotic. Could we soften it?",
            TicketPriority.Low,
            uid("frank@acme.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),
    ];

    private static IReadOnlyList<TicketScenario> GlobexTicketScenarios(Func<string, Guid?> uid) =>
    [
        new("Need help wiring our Salesforce integration",
            "We're trying to set up the inbound webhook but it keeps returning 401. Maybe a tenant header thing?",
            TicketPriority.Medium,
            uid("dave@globex.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),

        new("Export to CSV truncates long descriptions",
            "Product descriptions over ~500 chars get cut off in the CSV download.",
            TicketPriority.Low,
            uid("dave@globex.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),
    ];
}
