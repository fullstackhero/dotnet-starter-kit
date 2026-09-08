using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Identity.EntityFrameworkCore;
using FSH.Framework.Persistence.Providers;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Data;

public class IdentityDbContext : MultiTenantIdentityDbContext<FshUser,
    FshRole,
    string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    FshRoleClaim,
    IdentityUserToken<string>,
    IdentityUserPasskey<string>>
{
    private readonly DatabaseOptions _settings;
    private new AppTenantInfo TenantInfo { get; set; }
    private readonly IHostEnvironment _environment;
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<ImpersonationGrant> ImpersonationGrants => Set<ImpersonationGrant>();

    public IdentityDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<IdentityDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);

        _environment = environment;
        _settings = settings.Value;
        TenantInfo = multiTenantContextAccessor.MultiTenantContext.TenantInfo!;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // The outbox/inbox tables are framework infrastructure, owned by EventingDbContext (issue #1349).

        // Default-on tenant isolation: non-IGlobalEntity entities get IsMultiTenant() automatically (ImpersonationGrant opts out).
        // Identity tables are already IsMultiTenant in IdentityConfigurations.cs; auto-apply detects that annotation and skips them.
        builder.ApplyTenantIsolationByDefault();
    }

    /// <summary>
    /// This context does not derive from BaseDbContext, so it registers the framework's provider
    /// conventions itself — without them the portable column intent is never resolved.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.AddHeroProviderConventions(DbProviderResolver.FromEfProviderName(Database.ProviderName));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureHeroDatabase(
                _settings.Provider,
                TenantInfo.ConnectionString,
                _settings.MigrationsAssembly,
                _environment.IsDevelopment());
        }
    }
}