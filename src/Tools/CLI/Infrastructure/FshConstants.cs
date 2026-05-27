namespace FSH.CLI.Infrastructure;

internal static class FshConstants
{
    // NuGet package identifiers
    internal const string CliPackageId = "FullStackHero.CLI";
    internal const string TemplatePackageId = "FullStackHero.NET.StarterKit";
    internal const string TemplateShortName = "fsh";

    // URLs
    internal const string NuGetFlatContainerUrl = "https://api.nuget.org/v3-flatcontainer";
    internal const string GitHubRepoUrl = "https://github.com/fullstackhero/dotnet-starter-kit";
    internal const string ReleaseNotesUrl = $"{GitHubRepoUrl}/releases";
    internal const string DocsUrl = "https://fullstackhero.net";

    // Default ports
    internal const int ApiHttpPort = 5030;
    internal const int ApiHttpsPort = 7030;
    internal const int AspireDashboardPort = 15888;

    // Theming
    internal const string AccentColor = "dodgerblue1";
    internal const string SuccessColor = "green";
    internal const string WarningColor = "yellow";
    internal const string ErrorColor = "red";
    internal const string DimColor = "dim";

    // Reserved names that cannot be used as project names
    internal static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        "FSH", "System", "Microsoft", "NuGet"
    };
}
