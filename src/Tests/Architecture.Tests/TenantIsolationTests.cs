using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Domain;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Enforces the "tenant-isolated by default" contract introduced in 2026-05-17.
/// Every concrete entity reachable from a <see cref="BaseDbContext"/>-derived
/// DbContext must end up with Finbuckle's <c>IsMultiTenant()</c> annotation
/// applied — either explicitly in an <c>IEntityTypeConfiguration</c> or via
/// the auto-apply in <c>BaseDbContext.OnModelCreating</c>. Opt-out is via the
/// <see cref="IGlobalEntity"/> marker interface (used by platform-wide rows
/// like BillingPlan, ImpersonationGrant, OutboxMessage, InboxMessage).
///
/// This test catches the silent-leak class of bug where someone adds a new
/// entity to a module, forgets to mark it multitenant, and ships it — the
/// orphan rows end up visible to every tenant.
/// </summary>
public sealed class TenantIsolationTests
{
    private const string FinbuckleAnnotation = "Finbuckle:MultiTenant";

    /// <summary>
    /// Every <see cref="BaseDbContext"/>-derived DbContext in the loaded module
    /// assemblies is instantiated; we assert that every non-owned entity in its
    /// model either has the Finbuckle annotation or implements <see cref="IGlobalEntity"/>.
    /// </summary>
    [Fact]
    public void BaseDbContext_Entities_Should_Be_TenantIsolated_Or_Marked_Global()
    {
        var violations = new List<string>();

        foreach (var ctxType in DiscoverBaseDbContextTypes())
        {
            using var ctx = ConstructDbContext(ctxType);
            var model = ctx.Model;

            foreach (var entityType in model.GetEntityTypes())
            {
                if (entityType.IsOwned()) continue;
                if (entityType.ClrType is null) continue;
                if (entityType.FindPrimaryKey() is null) continue;
                if (typeof(IGlobalEntity).IsAssignableFrom(entityType.ClrType)) continue;

                if (entityType.FindAnnotation(FinbuckleAnnotation) is null)
                {
                    violations.Add($"{ctxType.Name} → {entityType.ClrType.FullName} is missing IsMultiTenant() and not marked IGlobalEntity");
                }
            }
        }

        violations.ShouldBeEmpty(
            "Every entity in a BaseDbContext-derived DbContext must be tenant-isolated. " +
            "Apply via builder.IsMultiTenant() in EF config, OR opt out by implementing " +
            "FSH.Framework.Core.Domain.IGlobalEntity (only for entities that are genuinely " +
            "platform-wide, like BillingPlan or ImpersonationGrant). " +
            $"Violations:\n  {string.Join("\n  ", violations)}");
    }

    private static IEnumerable<Type> DiscoverBaseDbContextTypes()
    {
        return ModuleAssemblyDiscovery.GetModuleAssemblies()
            .SelectMany(a => SafeGetTypes(a))
            .Where(t => t.IsClass && !t.IsAbstract && typeof(BaseDbContext).IsAssignableFrom(t));
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t is not null)!; }
    }

    /// <summary>
    /// BaseDbContext takes (accessor, options, settings, environment). We stub
    /// each with the minimum surface needed to reach OnModelCreating. The
    /// concrete DbContext type drives DbContextOptions&lt;T&gt; via reflection
    /// so we don't hand-roll one per module.
    /// </summary>
    private static DbContext ConstructDbContext(Type dbContextType)
    {
        var optionsType = typeof(DbContextOptions<>).MakeGenericType(dbContextType);
        var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;
        // Use Npgsql provider so OnConfiguring's per-tenant wiring is a no-op
        // (we pass an empty ConnectionString below). Model is built lazily on
        // first access of ctx.Model — no actual DB connection is opened.
        builder.UseNpgsql("Host=arch;Database=arch;Username=arch;Password=arch");
        var options = builder.Options;

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        var ctor = dbContextType.GetConstructor([
            typeof(IMultiTenantContextAccessor<AppTenantInfo>),
            optionsType,
            typeof(IOptions<DatabaseOptions>),
            typeof(IHostEnvironment),
        ]) ?? throw new InvalidOperationException(
            $"{dbContextType.Name} does not have the expected BaseDbContext constructor signature.");

        return (DbContext)ctor.Invoke([
            new StubAccessor(),
            options,
            settings,
            new StubEnvironment(),
        ]);
    }

    private sealed class StubAccessor : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext<AppTenantInfo> MultiTenantContext { get; set; } =
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("arch", "arch", string.Empty, "arch@arch", "arch"));

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "arch";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
