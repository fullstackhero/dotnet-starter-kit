namespace FSH.Framework.Core.Context;

/// <summary>
/// Provides access to HTTP request context information without direct dependency on ASP.NET Core.
/// Use this interface in handlers that need request metadata for auditing, logging, etc.
/// </summary>
public interface IRequestContext
{
    /// <summary>
    /// Gets the remote IP address of the client making the request.
    /// </summary>
    string? IpAddress { get; }

    /// <summary>
    /// Gets the User-Agent header from the request.
    /// </summary>
    string? UserAgent { get; }

    /// <summary>
    /// Gets a client identifier from the X-Client-Id header, or a default value.
    /// </summary>
    string ClientId { get; }

    /// <summary>
    /// Gets the origin URL (scheme + host + path base) of the current request.
    /// </summary>
    string? Origin { get; }
}