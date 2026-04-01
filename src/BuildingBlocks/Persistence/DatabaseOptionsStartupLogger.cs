using FSH.Framework.Shared.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Persistence;

/// <summary>
/// Hosted service that logs database configuration options during application startup.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DatabaseOptionsStartupLogger"/> class.
/// </remarks>
/// <param name="logger">Logger instance for writing startup information.</param>
/// <param name="options">Database configuration options.</param>
public sealed class DatabaseOptionsStartupLogger(
    ILogger<DatabaseOptionsStartupLogger> logger,
    IOptions<DatabaseOptions> options) : IHostedService
{
    private readonly IOptions<DatabaseOptions> _options = options;

    /// <summary>
    /// Logs database configuration information when the service starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opt = _options.Value;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("current db provider: {Provider}", opt.Provider);
            logger.LogInformation("for docs: https://www.fullstackhero.net");
            logger.LogInformation("sponsor: https://opencollective.com/fullstackhero");
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs no operation when the service stops.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}