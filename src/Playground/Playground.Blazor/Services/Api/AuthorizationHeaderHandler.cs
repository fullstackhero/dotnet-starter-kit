using FSH.Playground.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using System.Net;

namespace FSH.Playground.Blazor.Services.Api;

/// <summary>
/// Delegating handler that adds the JWT token to API requests and handles 401 responses
/// by attempting to refresh the access token. If refresh fails, signs out the user and
/// notifies Blazor components via IAuthStateNotifier.
/// </summary>
internal sealed class AuthorizationHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICircuitTokenCache _circuitTokenCache;
    private readonly ILogger<AuthorizationHeaderHandler> _logger;

    /// <summary>
    /// Track if sign-out has already been initiated to prevent multiple sign-out attempts.
    /// This is scoped per circuit (instance field, not static).
    /// </summary>
    private bool _signOutInitiated;

    public AuthorizationHeaderHandler(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ICircuitTokenCache circuitTokenCache,
        ILogger<AuthorizationHeaderHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _circuitTokenCache = circuitTokenCache;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get current access token from circuit cache or claims
        var accessToken = await GetAccessTokenAsync();

        // Attach access token to request
        if (!string.IsNullOrEmpty(accessToken))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        }

        // Send the request
        var response = await base.SendAsync(request, cancellationToken);

        // If we get a 401, try to refresh the token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // If sign-out already initiated, don't attempt refresh or sign-out again
            if (_signOutInitiated)
            {
                return response;
            }

            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogDebug("Received 401 but no access token available - cannot refresh");
                return response;
            }

            _logger.LogInformation("Received 401, attempting token refresh");

            var newAccessToken = await TryRefreshTokenAsync(cancellationToken);

            if (!string.IsNullOrEmpty(newAccessToken))
            {
                _logger.LogInformation("Token refresh successful, retrying request");

                // Clone the request with new token
                using var retryRequest = await CloneHttpRequestMessageAsync(request);
                retryRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newAccessToken);

                // Dispose the original response before retrying
                response.Dispose();

                // Retry the request with the new token
                response = await base.SendAsync(retryRequest, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Token refresh failed, signing out user");

                // Mark sign-out as initiated to prevent multiple sign-out attempts
                _signOutInitiated = true;

                // Sign out the user since refresh token is also invalid/expired
                await SignOutUserAsync();
            }
        }

        return response;
    }

    private async Task SignOutUserAsync()
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                // Try to sign out via cookies, but this may fail in Blazor Server's
                // SignalR context where the response has already started
                try
                {
                    if (!httpContext.Response.HasStarted)
                    {
                        await httpContext.SignOutAsync("Cookies");
                        _logger.LogInformation("User signed out due to expired refresh token");
                    }
                    else
                    {
                        _logger.LogDebug("Response already started, skipping cookie sign-out");
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // Expected in Blazor Server SignalR context - headers are read-only
                    _logger.LogDebug(ex, "Could not sign out via cookies (response started), using navigation redirect");
                }

                // Notify Blazor components that session has expired
                // This will trigger navigation to login page with forceLoad:true,
                // which will create a new HTTP request where cookies can be cleared
                var authStateNotifier = _serviceProvider.GetService<IAuthStateNotifier>();
                authStateNotifier?.NotifySessionExpired();
            }
        }
        catch (Microsoft.AspNetCore.Components.NavigationException ex)
        {
            // Expected - NavigateTo with forceLoad throws this to interrupt execution
            _logger.LogDebug(ex, "Navigation to login triggered (NavigationException is expected)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle session expiration");
        }
    }

    private Task<string?> GetAccessTokenAsync()
    {
        try
        {
            // First, check circuit-scoped cache for refreshed tokens
            // This is critical because httpContext.User claims are cached per circuit
            // and don't update even after SignInAsync
            if (!string.IsNullOrEmpty(_circuitTokenCache.AccessToken))
            {
                return Task.FromResult<string?>(_circuitTokenCache.AccessToken);
            }

            // Fall back to claims (initial token from cookie)
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            if (user?.Identity?.IsAuthenticated == true)
            {
                return Task.FromResult(user.FindFirst("access_token")?.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get access token");
        }

        return Task.FromResult<string?>(null);
    }

    private async Task<string?> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Resolve the token refresh service from the service provider
            // We use IServiceProvider to avoid circular dependency issues
            var tokenRefreshService = _serviceProvider.GetService<ITokenRefreshService>();
            if (tokenRefreshService is null)
            {
                _logger.LogWarning("TokenRefreshService is not registered");
                return null;
            }

            return await tokenRefreshService.TryRefreshTokenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return null;
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        // Copy headers (except Authorization which we'll set separately)
        foreach (var header in request.Headers.Where(h => !string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)))
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Copy content if present
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            // Copy content headers
            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        // Copy options
        foreach (var option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }

        return clone;
    }
}
