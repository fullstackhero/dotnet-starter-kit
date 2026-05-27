using System.Net.NetworkInformation;
using FSH.CLI.Infrastructure;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FSH.CLI.Commands;

public sealed class DoctorCommand : AsyncCommand
{
    protected override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine($"[bold {FshConstants.AccentColor}]FSH Doctor[/] — checking your development environment");
        AnsiConsole.WriteLine();

        var checks = new List<DoctorCheck>();

        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse(FshConstants.AccentColor))
            .StartAsync("Running checks...", async _ =>
            {
                checks.Add(await CheckDotNetSdkAsync(cancellationToken).ConfigureAwait(false));
                checks.Add(await CheckGitAsync(cancellationToken).ConfigureAwait(false));
                checks.Add(await CheckDockerAsync(cancellationToken).ConfigureAwait(false));
                checks.Add(await CheckAspireWorkloadAsync(cancellationToken).ConfigureAwait(false));
                checks.Add(await CheckFshTemplateAsync(cancellationToken).ConfigureAwait(false));
                checks.Add(CheckPorts());
            }).ConfigureAwait(false);

        // Render results
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Check")
            .AddColumn("Status")
            .AddColumn("Details");

        foreach (DoctorCheck check in checks)
        {
            string statusMarkup = check.Status switch
            {
                CheckStatus.Pass => $"[{FshConstants.SuccessColor}]PASS[/]",
                CheckStatus.Warn => $"[{FshConstants.WarningColor}]WARN[/]",
                CheckStatus.Fail => $"[{FshConstants.ErrorColor}]FAIL[/]",
                _ => "[dim]?[/]"
            };

            table.AddRow(check.Name, statusMarkup, check.Detail.EscapeMarkup());
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        bool hasFailures = checks.Exists(c => c.Status == CheckStatus.Fail);
        bool hasWarnings = checks.Exists(c => c.Status == CheckStatus.Warn);

        if (!hasFailures && !hasWarnings)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.SuccessColor}]All checks passed. You're ready to go![/]");
        }
        else if (hasFailures)
        {
            AnsiConsole.MarkupLine($"[{FshConstants.ErrorColor}]Some required tools are missing. Install them before continuing.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[{FshConstants.WarningColor}]Some optional checks need attention. See details above.[/]");
        }

        return hasFailures ? 1 : 0;
    }

    private static async Task<DoctorCheck> CheckDotNetSdkAsync(CancellationToken cancellationToken)
    {
        (bool ok, string version) = await ProcessRunner.CaptureAsync("dotnet", "--version", cancellationToken)
            .ConfigureAwait(false);

        if (!ok) return new(".NET SDK", CheckStatus.Fail, "Not found. Install from https://dotnet.microsoft.com");

        bool supported = version.StartsWith("10.", StringComparison.Ordinal);
        return new(".NET SDK", supported ? CheckStatus.Pass : CheckStatus.Fail,
            supported ? $"v{version}" : $"v{version} (requires .NET 10+)");
    }

    private static async Task<DoctorCheck> CheckGitAsync(CancellationToken cancellationToken)
    {
        (bool ok, string version) = await ProcessRunner.CaptureAsync("git", "--version", cancellationToken)
            .ConfigureAwait(false);

        return new("Git", ok ? CheckStatus.Pass : CheckStatus.Warn,
            ok ? version : "Not found. Install from https://git-scm.com");
    }

    private static async Task<DoctorCheck> CheckDockerAsync(CancellationToken cancellationToken)
    {
        (bool installed, string version) = await ProcessRunner.CaptureAsync("docker", "--version", cancellationToken)
            .ConfigureAwait(false);

        if (!installed)
            return new("Docker", CheckStatus.Fail, "Not found. Install from https://docker.com");

        (bool running, _) = await ProcessRunner.CaptureAsync("docker", "info", cancellationToken)
            .ConfigureAwait(false);

        if (!running)
            return new("Docker", CheckStatus.Warn, "Installed but not running. Start Docker Desktop.");

        return new("Docker", CheckStatus.Pass, version.Split('\n')[0]);
    }

    private static async Task<DoctorCheck> CheckAspireWorkloadAsync(CancellationToken cancellationToken)
    {
        (bool ok, string output) = await ProcessRunner.CaptureAsync("dotnet", "workload list", cancellationToken)
            .ConfigureAwait(false);

        bool installed = ok && output.Contains("aspire", StringComparison.OrdinalIgnoreCase);
        return new("Aspire Workload", installed ? CheckStatus.Pass : CheckStatus.Warn,
            installed ? "Installed" : "Run: dotnet workload install aspire");
    }

    private static async Task<DoctorCheck> CheckFshTemplateAsync(CancellationToken cancellationToken)
    {
        string? version = await NuGetClient.GetInstalledTemplateVersionAsync(cancellationToken)
            .ConfigureAwait(false);

        return new("FSH Template", version is not null ? CheckStatus.Pass : CheckStatus.Warn,
            version is not null
                ? $"v{version}"
                : $"Not installed. Run: dotnet new install {FshConstants.TemplatePackageId}");
    }

    private static DoctorCheck CheckPorts()
    {
        int[] ports = [FshConstants.ApiHttpPort, FshConstants.ApiHttpsPort, FshConstants.AspireDashboardPort];
        var inUse = new List<int>();

        TcpConnectionInformation[] connections;
        try
        {
            connections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
        }
        catch
        {
            return new("Ports", CheckStatus.Pass, "Could not check (assuming available)");
        }

        inUse.AddRange(ports.Where(port =>
            Array.Exists(connections, c => c.LocalEndPoint.Port == port)));

        if (inUse.Count == 0)
            return new("Ports", CheckStatus.Pass, $"All available ({string.Join(", ", ports)})");

        return new("Ports", CheckStatus.Warn, $"In use: {string.Join(", ", inUse)}");
    }

    private sealed record DoctorCheck(string Name, CheckStatus Status, string Detail);

    private enum CheckStatus { Pass, Warn, Fail }
}
