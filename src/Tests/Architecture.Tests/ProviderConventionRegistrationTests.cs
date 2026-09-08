using System.Reflection;
using FSH.Framework.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// Every DbContext must have the framework's provider conventions applied, or its portable column
/// and index intent silently never resolves.
/// </summary>
/// <remarks>
/// <para>
/// Contexts deriving from <see cref="BaseDbContext"/> inherit the registration. The three that
/// cannot — <c>IdentityDbContext</c>, <c>TenantDbContext</c> and <c>BillingDbContext</c>, each with
/// its own EF base class — must override <c>ConfigureConventions</c> themselves.
/// </para>
/// <para>
/// This guards a failure mode that is invisible at build time and easy to miss in review: a context
/// without the conventions produces <c>text</c> columns where the model asked for JSON, drops every
/// partial-index filter, and leaks <c>Fsh:*</c> annotations into the migrations snapshot.
/// </para>
/// </remarks>
public class ProviderConventionRegistrationTests
{
    private static List<Type> NonBaseDbContexts() =>
        ModuleAssemblyDiscovery.GetModuleAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false }
                && typeof(DbContext).IsAssignableFrom(t)
                && !typeof(BaseDbContext).IsAssignableFrom(t))
            .ToList();

    private static bool DeclaresConfigureConventions(Type context) =>
        context.GetMethod(
            "ConfigureConventions",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
            [typeof(ModelConfigurationBuilder)]) is not null;

    [Fact]
    public void Every_DbContext_Should_Derive_From_BaseDbContext_Or_Register_Provider_Conventions()
    {
        var offenders = NonBaseDbContexts()
            .Where(c => !DeclaresConfigureConventions(c))
            .Select(c => c.FullName!)
            .ToList();

        offenders.ShouldBeEmpty(
            "these DbContexts neither derive from BaseDbContext nor override ConfigureConventions, so "
            + "AddHeroProviderConventions never runs for them: " + string.Join(", ", offenders));
    }

    [Fact]
    public void The_Guard_Should_Actually_See_The_Contexts_It_Polices()
    {
        // Without this, the test above passes vacuously if assembly discovery or the reflection
        // lookup ever stops finding anything — which is exactly when it is most needed.
        var names = NonBaseDbContexts().Select(c => c.Name).ToList();

        names.ShouldContain("IdentityDbContext");
        names.ShouldContain("TenantDbContext");
        names.ShouldContain("BillingDbContext");
        NonBaseDbContexts().ShouldAllBe(c => DeclaresConfigureConventions(c));
    }
}
