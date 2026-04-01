using Mediator;
using System.Diagnostics;

namespace FSH.Framework.Web.Observability.OpenTelemetry;

/// <summary>
/// Emits spans around Mediator commands/queries to improve trace visibility.
/// </summary>
public sealed class MediatorTracingBehavior<TMessage, TResponse>(ActivitySource activitySource) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(TMessage message, MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(next);

        using var activity = activitySource.StartActivity(
            $"Mediator {typeof(TMessage).Name}",
            ActivityKind.Internal);

        activity?.SetTag("mediator.request_type", typeof(TMessage).FullName);

        try
        {
            var response = await next(message, cancellationToken);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return response;
        }
        // Broad catch is intentional: tracing must record all exception types
        // on the activity span before re-throwing to the caller.
        catch (Exception ex)
        {
            if (activity is not null)
            {
                activity.SetStatus(ActivityStatusCode.Error);
                activity.SetTag("exception.type", ex.GetType().FullName);
                activity.SetTag("exception.message", ex.Message);
            }

            throw;
        }
    }
}