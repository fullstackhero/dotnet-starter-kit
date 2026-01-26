using FSH.Framework.Core.Context;

namespace FSH.Modules.Identity.Contracts.Services;

/// <summary>
/// Service interface for accessing HTTP request context information.
/// Provides request metadata for auditing, logging, and other cross-cutting concerns.
/// </summary>
public interface IRequestContextService : IRequestContext
{
}
