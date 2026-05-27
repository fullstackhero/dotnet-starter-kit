using FSH.Framework.Eventing.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;

namespace FSH.Framework.Eventing.RabbitMq;

/// <summary>
/// RabbitMQ-based event bus implementation for distributed deployments.
/// Publishes integration events to a fanout exchange.
/// </summary>
public sealed partial class RabbitMqEventBus : IEventBus, IAsyncDisposable
{
    private readonly IEventSerializer _serializer;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _disposed;

    public RabbitMqEventBus(
        IEventSerializer serializer,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqEventBus> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _serializer = serializer;
        _logger = logger;
        _options = options.Value;
    }

    public async Task PublishAsync(IIntegrationEvent @event, CancellationToken ct = default)
    {
        await PublishAsync([@event], ct).ConfigureAwait(false);
    }

    public async Task PublishAsync(IEnumerable<IIntegrationEvent> events, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        await EnsureConnectionAsync(ct).ConfigureAwait(false);

        foreach (var @event in eventList)
        {
            await PublishSingleAsync(@event, ct).ConfigureAwait(false);
        }
    }

    private async Task PublishSingleAsync(IIntegrationEvent @event, CancellationToken ct)
    {
        var eventType = @event.GetType();
        var routingKey = eventType.FullName ?? eventType.Name;

        var payload = _serializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(payload);

        var retryCount = 0;
        var maxRetries = _options.PublishRetryCount;
        var retryDelay = TimeSpan.FromMilliseconds(_options.PublishRetryDelayMs);

        while (true)
        {
            try
            {
                if (_channel is null)
                {
                    throw new InvalidOperationException("RabbitMQ channel is not initialized.");
                }

                var properties = new BasicProperties
                {
                    MessageId = @event.Id.ToString(),
                    Timestamp = new AmqpTimestamp(new DateTimeOffset(@event.OccurredOnUtc).ToUnixTimeSeconds()),
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    CorrelationId = @event.CorrelationId,
                    Headers = new Dictionary<string, object?>
                    {
                        ["event-type"] = eventType.AssemblyQualifiedName,
                        ["tenant-id"] = @event.TenantId,
                        ["source"] = @event.Source
                    }
                };

                await _channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct).ConfigureAwait(false);

                LogPublished(routingKey, @event.Id, _options.ExchangeName);

                return;
            }
            // Broad catch with retry guard: any publish failure triggers reconnection and retry.
            catch (Exception ex) when (retryCount < maxRetries)
            {
                retryCount++;
                _logger.LogWarning(
                    ex,
                    "Failed to publish event {EventId}, retrying ({RetryCount}/{MaxRetries})",
                    @event.Id, retryCount, maxRetries);

                await Task.Delay(retryDelay, ct).ConfigureAwait(false);

                // Try to reconnect
                await ReconnectAsync(ct).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
        {
            return;
        }

        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return;
            }

            await CreateConnectionAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ReconnectAsync(CancellationToken ct)
    {
        await _connectionLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            await CreateConnectionAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task CreateConnectionAsync(CancellationToken ct)
    {
        LogConnecting(_options.Host, _options.Port, _options.VirtualHost);

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost
        };

        if (_options.UseSsl)
        {
            factory.Ssl = new SslOption { Enabled = true };
        }

        _connection = await factory.CreateConnectionAsync(ct).ConfigureAwait(false);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

        // Declare a durable topic exchange
        await _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: ct).ConfigureAwait(false);

        LogConnected(_options.ExchangeName);
    }

    private async Task DisposeConnectionAsync()
    {
        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync().ConfigureAwait(false);
                _channel.Dispose();
            }
            // Cleanup must not throw: connection may already be closed or faulted
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing RabbitMQ channel during cleanup");
            }

            _channel = null;
        }

        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync().ConfigureAwait(false);
                _connection.Dispose();
            }
            // Cleanup must not throw: connection may already be closed or faulted
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error closing RabbitMQ connection during cleanup");
            }

            _connection = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await DisposeConnectionAsync().ConfigureAwait(false);
        _connectionLock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Published integration event {EventType} ({EventId}) to exchange {Exchange}")]
    private partial void LogPublished(string eventType, Guid eventId, string exchange);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connecting to RabbitMQ at {Host}:{Port}/{VirtualHost}")]
    private partial void LogConnecting(string host, int port, string virtualHost);

    [LoggerMessage(Level = LogLevel.Information, Message = "RabbitMQ connection established. Exchange: {Exchange}")]
    private partial void LogConnected(string exchange);
}