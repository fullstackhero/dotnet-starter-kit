namespace FSH.Framework.Eventing.RabbitMq;

/// <summary>
/// Configuration options for RabbitMQ event bus.
/// </summary>
public sealed class RabbitMqOptions
{
    /// <summary>
    /// RabbitMQ host name or connection string.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// RabbitMQ port. Default is 5672.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Username for RabbitMQ authentication.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// Password for RabbitMQ authentication.
    /// </summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Virtual host. Default is "/".
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Exchange name for publishing events. Default is "fsh.events".
    /// </summary>
    public string ExchangeName { get; set; } = "fsh.events";

    /// <summary>
    /// Queue name prefix for consuming events. Default is "fsh".
    /// </summary>
    public string QueuePrefix { get; set; } = "fsh";

    /// <summary>
    /// Enable SSL/TLS connection. Default is false.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Number of retry attempts for publishing. Default is 3.
    /// </summary>
    public int PublishRetryCount { get; set; } = 3;

    /// <summary>
    /// Delay between retries in milliseconds. Default is 1000.
    /// </summary>
    public int PublishRetryDelayMs { get; set; } = 1000;
}