using FSH.Framework.Core.Messaging.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.Events;

public class InMemoryEventBus : IEventBus
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryEventBus(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        using var scope = _serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            const int maxAttempts = 3;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    attempt++;
                    await handler.HandleAsync(@event, cancellationToken);
                    break; // Success
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts)
                    {
                        Console.WriteLine($"❌ Handler for {typeof(TEvent).Name} failed after {attempt} attempts: {ex.Message}");
                        // Optionally: Add to dead-letter queue
                    }
                    else
                    {
                        await Task.Delay(100 * attempt, cancellationToken); // simple backoff
                    }
                }
            }
        }
    }
}
