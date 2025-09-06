namespace FSH.Framework.Infrastructure.Mediator;

public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for {requestType.Name}");
        }

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.HandleAsync));
        if (method == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");
        }

        var result = await (Task<TResponse>)method.Invoke(handler, new object[] { request, cancellationToken })!;
        return result;
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);

        var handler = _serviceProvider.GetService(handlerType);
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for {requestType.Name}");
        }

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest>.HandleAsync));
        if (method == null)
        {
            throw new InvalidOperationException($"HandleAsync method not found on {handlerType.Name}");
        }

        await (Task)method.Invoke(handler, new object[] { request, cancellationToken })!;
    }
}



