namespace FSH.Framework.Core.CQRS;

// Marker for query requests (intended to return data without modifying state)
public interface IQuery<TResponse> : IRequest<TResponse> { }
