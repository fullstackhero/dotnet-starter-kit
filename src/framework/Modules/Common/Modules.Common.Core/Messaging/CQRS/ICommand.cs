namespace FSH.Modules.Common.Core.Messaging.CQRS;

// Marker for command requests (intended to modify system state)
public interface ICommand<TResponse> : IRequest<TResponse> { }