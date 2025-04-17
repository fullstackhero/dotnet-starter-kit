using FSH.Framework.Core.Messaging.Events;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Infrastructure.Messaging.Events;

public class InMemoryEventPublisher : IEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public InMemoryEventPublisher(IServiceProvider serviceProvider) =>
        _serviceProvider = serviceProvider;

    public async Task PublishAsync(IEvent appEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(appEvent);

        using var scope = _serviceProvider.CreateScope();

        // Get handler type dynamically based on the event's runtime type
        var handlerType = typeof(IEventHandler<>).MakeGenericType(appEvent.GetType());
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            const int maxAttempts = 3;
            int attempt = 0;

            while (attempt < maxAttempts)
            {
                try
                {
                    attempt++;
                    var method = handlerType.GetMethod("HandleAsync");

                    if (method is not null)
                    {
                        await (Task)method.Invoke(handler, new object[] { appEvent, cancellationToken })!;
                    }

                    break; // Success
                }
                catch (Exception ex)
                {
                    if (attempt == maxAttempts)
                    {
                        Console.WriteLine($"Handler for {typeof(IEvent).Name} failed after {attempt} attempts: {ex.Message}");
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