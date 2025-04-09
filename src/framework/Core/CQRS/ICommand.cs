namespace FSH.Framework.Core.CQRS;

// Marker for command requests (intended to modify system state)
public interface ICommand<TResponse> : IRequest<TResponse> { }
