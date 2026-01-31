using FSH.Framework.Shared.Persistence;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Persistence;

/// <summary>
/// Hosted service that logs database configuration options during application startup.
/// </summary>
public sealed class DatabaseOptionsStartupLogger : IHostedService
{
    private readonly ILogger<DatabaseOptionsStartupLogger> _logger;
    private readonly IOptions<DatabaseOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseOptionsStartupLogger"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for writing startup information.</param>
    /// <param name="options">Database configuration options.</param>
    public DatabaseOptionsStartupLogger(
        ILogger<DatabaseOptionsStartupLogger> logger,
        IOptions<DatabaseOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Logs database configuration information when the service starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        _logger.LogInformation("current db provider: {Provider}", options.Provider);
        _logger.LogInformation("for docs: https://www.fullstackhero.net");
        _logger.LogInformation("sponsor: https://opencollective.com/fullstackhero");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Performs no operation when the service stops.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

