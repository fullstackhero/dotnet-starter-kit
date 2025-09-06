namespace FSH.Framework.Infrastructure.Mediator;

public interface IMediator
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task SendAsync(IRequest request, CancellationToken cancellationToken = default);
}
