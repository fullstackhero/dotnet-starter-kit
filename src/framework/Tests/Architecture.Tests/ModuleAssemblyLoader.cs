using System.Reflection;

namespace Architecture.Tests;
public static class ModuleAssemblyLoader
{
    private static bool _loaded;

    public static void EnsureModulesLoaded()
    {
        if (_loaded) return;

        // ✅ Explicitly reference one or more types from each module to force load
        _ = typeof(FSH.Modules.Identity.IdentityModule).Assembly;
        _ = typeof(FSH.Modules.Tenant.TenantModule).Assembly;
        _ = typeof(FSH.Modules.Auditing.AuditingModule).Assembly;
        // Add more modules here...

        _loaded = true;
    }

    public static IEnumerable<Assembly> GetFshAssemblies()
    {
        EnsureModulesLoaded();

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a =>
                !a.IsDynamic &&
                a.FullName?.StartsWith("FSH", StringComparison.Ordinal) == true);
    }
}