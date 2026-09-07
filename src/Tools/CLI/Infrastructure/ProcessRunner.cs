using System.Diagnostics;
using Spectre.Console;

namespace FSH.CLI.Infrastructure;

/// <summary>
/// Shared process execution with cancellation, timeout, and output streaming.
/// </summary>
internal static class ProcessRunner
{
    /// <summary>
    /// Runs a process and returns its exit code. Output is streamed to the console in real-time.
    /// </summary>
    internal static async Task<int> RunAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        bool showOutput = true,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        using var process = Process.Start(psi);
        if (process is null) return 1;

        using var registration = cancellationToken.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }
        });

        Task outputTask = showOutput
            ? StreamOutputAsync(process.StandardOutput, FshConstants.DimColor)
            : process.StandardOutput.ReadToEndAsync(cancellationToken);

        Task errorTask = showOutput
            ? StreamOutputAsync(process.StandardError, FshConstants.ErrorColor)
            : process.StandardError.ReadToEndAsync(cancellationToken);

        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return process.ExitCode;
    }

    /// <summary>
    /// Runs a process and captures its stdout. Returns (exitCode == 0, stdout).
    /// </summary>
    internal static async Task<(bool success, string output)> CaptureAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process is null) return (false, string.Empty);

            using var registration = cancellationToken.Register(() =>
            {
                try { process.Kill(entireProcessTree: true); }
                catch { /* process may have already exited */ }
            });

            // Drain both pipes concurrently. stderr is redirected, so leaving it unread can
            // deadlock a child that writes enough to fill the pipe buffer.
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            return (process.ExitCode == 0, (await outputTask.ConfigureAwait(false)).Trim());
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty);
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Runs a process and captures stdout, stderr and the exit code separately, for callers
    /// that need to show the user why something failed rather than just that it did.
    /// </summary>
    internal static async Task<(int exitCode, string output, string error)> CaptureWithErrorAsync(
        string fileName,
        string arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
        };

        using var process = Process.Start(psi);
        if (process is null) return (1, string.Empty, string.Empty);

        using var registration = cancellationToken.Register(() =>
        {
            try { process.Kill(entireProcessTree: true); }
            catch { /* process may have already exited */ }
        });

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(outputTask, errorTask).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return (process.ExitCode,
                (await outputTask.ConfigureAwait(false)).Trim(),
                (await errorTask.ConfigureAwait(false)).Trim());
    }

    private static async Task StreamOutputAsync(StreamReader reader, string color)
    {
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                AnsiConsole.MarkupLine($"  [{color}]{line.EscapeMarkup()}[/]");
            }
        }
    }
}
