using FSH.Modules.Identity.Contracts.DTOs;
using System.Security.Claims;

namespace FSH.Modules.Identity.Contracts.Services;

public interface ITokenService
{
    /// <summary>
    /// Issues a new access and refresh token for the specified subject.
    /// </summary>
    Task<TokenResponse> IssueAsync(
        string subject,
        IEnumerable<Claim> claims,
        string? tenant = null,
        CancellationToken ct = default);

    /// <summary>
    /// Issues a short-lived access token without a refresh token. Used by flows (e.g. impersonation)
    /// where refresh is deliberately disallowed. Pass <paramref name="lifetime"/> to override the
    /// default <c>JwtOptions.AccessTokenMinutes</c> (impersonation uses this to let the operator
    /// pick 10/15/30 min sessions).
    /// </summary>
    Task<(string AccessToken, DateTime ExpiresAtUtc)> IssueAccessOnlyAsync(
        string subject,
        IEnumerable<Claim> claims,
        TimeSpan? lifetime = null,
        CancellationToken ct = default);
}