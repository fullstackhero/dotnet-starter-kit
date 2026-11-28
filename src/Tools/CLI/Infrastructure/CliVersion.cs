using System.Reflection;

namespace FSH.CLI.Infrastructure;

internal static class CliVersion
{
    /// <summary>
    /// The CLI's version without the <c>+gitsha</c> build-metadata suffix the SDK appends to the
    /// informational version (e.g. <c>10.0.1</c>, not <c>10.0.1+2b7fe496…</c>).
    /// </summary>
    public static string Current { get; } =
        typeof(CliVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0]
        ?? typeof(CliVersion).Assembly.GetName().Version?.ToString(3)
        ?? "unknown";
}
