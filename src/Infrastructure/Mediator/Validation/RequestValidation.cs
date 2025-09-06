namespace FSH.Framework.Infrastructure.Mediator.Validation;

internal sealed class RequestValidation : IMediator
{
    private readonly IMediator _inner;
    private readonly IServiceProvider _serviceProvider;

    public RequestValidation(IMediator inner, IServiceProvider serviceProvider)
    {
        _inner = inner;
        _serviceProvider = serviceProvider;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(request, _serviceProvider, cancellationToken);
        return await _inner.SendAsync(request, cancellationToken);
    }

    public async Task SendAsync(IRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateAsync(request, _serviceProvider, cancellationToken);
        await _inner.SendAsync(request, cancellationToken);
    }
}