using FSH.Framework.Core.Context;
using FSH.Framework.Web.Origin;
using FSH.Modules.Identity.Contracts.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Provides HTTP request context information through an abstraction.
/// This allows handlers to access request metadata without direct ASP.NET Core dependencies.
/// </summary>
internal sealed class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly Uri? _originUrl;

    public RequestContextService(
        IHttpContextAccessor httpContextAccessor,
        IOptions<OriginOptions> originOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _originUrl = originOptions.Value.OriginUrl;
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public string ClientId
    {
        get
        {
            var clientId = _httpContextAccessor.HttpContext?.Request.Headers["X-Client-Id"].ToString();
            return string.IsNullOrWhiteSpace(clientId) ? "web" : clientId;
        }
    }

    public string? Origin
    {
        get
        {
            if (_originUrl is not null)
            {
                return _originUrl.AbsoluteUri.TrimEnd('/');
            }

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request is not null && !string.IsNullOrWhiteSpace(request.Scheme) && request.Host.HasValue)
            {
                return $"{request.Scheme}://{request.Host.Value}{request.PathBase}".TrimEnd('/');
            }

            return null;
        }
    }
}
