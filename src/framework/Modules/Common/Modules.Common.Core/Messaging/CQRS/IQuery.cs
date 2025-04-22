using FSH.Modules.Common.Core.Messaging.CQRS;

namespace FSH.Framework.Core.Messaging.CQRS;

// Marker for query requests (intended to return data without modifying state)
public interface IQuery<TResponse> : IRequest<TResponse> { }