using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using System.Net.Http.Headers;

namespace FSH.Framework.Jobs;

public sealed class HangfireCustomBasicAuthenticationFilter : IDashboardAuthorizationFilter
{
    private const string AuthenticationScheme = "Basic";
    private readonly ILogger<HangfireCustomBasicAuthenticationFilter> _logger;
    public string User { get; set; } = default!;
    public string Pass { get; set; } = default!;

    public HangfireCustomBasicAuthenticationFilter()
        : this(new NullLogger<HangfireCustomBasicAuthenticationFilter>())
    {
    }

    public HangfireCustomBasicAuthenticationFilter(ILogger<HangfireCustomBasicAuthenticationFilter> logger) => _logger = logger;

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var header = httpContext.Request.Headers.Authorization!;

        if (MissingAuthorizationHeader(header))
        {
            _logger.LogInformation("Request is missing Authorization Header");
            SetChallengeResponse(httpContext);
            return false;
        }

        var authValues = AuthenticationHeaderValue.Parse(header!);

        if (NotBasicAuthentication(authValues))
        {
            _logger.LogInformation("Request is NOT BASIC authentication");
            SetChallengeResponse(httpContext);
            return false;
        }

        var tokens = ExtractAuthenticationTokens(authValues);

        if (tokens.AreInvalid())
        {
            _logger.LogInformation("Authentication tokens are invalid (empty, null, whitespace)");
            SetChallengeResponse(httpContext);
            return false;
        }

        if (tokens.CredentialsMatch(User, Pass))
        {
            _logger.LogInformation("Awesome, authentication tokens match configuration!");
            return true;
        }

        _logger.LogInformation("Hangfire dashboard authentication failed — credentials do not match configuration");

        SetChallengeResponse(httpContext);
        return false;
    }

    private static bool MissingAuthorizationHeader(StringValues header)
    {
        return string.IsNullOrWhiteSpace(header);
    }

    private static BasicAuthenticationTokens ExtractAuthenticationTokens(AuthenticationHeaderValue authValues)
    {
        string? parameter = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter!));
        string[]? parts = parameter.Split(':');
        return new BasicAuthenticationTokens(parts);
    }

    private static bool NotBasicAuthentication(AuthenticationHeaderValue authValues)
    {
        return !AuthenticationScheme.Equals(authValues.Scheme, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetChallengeResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
    }
}

public sealed class BasicAuthenticationTokens
{
    private readonly string[] _tokens;

    public string? Username => _tokens.Length > 0 ? _tokens[0] : null;
    public string? Password => _tokens.Length > 1 ? _tokens[1] : null;

    public BasicAuthenticationTokens(string[] tokens)
    {
        _tokens = tokens;
    }

    public bool AreInvalid()
    {
        return _tokens.Length != 2
            || string.IsNullOrWhiteSpace(_tokens[0])
            || string.IsNullOrWhiteSpace(_tokens[1]);
    }

    public bool CredentialsMatch(string user, string pass)
    {
        return string.Equals(Username, user, StringComparison.Ordinal)
            && string.Equals(Password, pass, StringComparison.Ordinal);
    }
}