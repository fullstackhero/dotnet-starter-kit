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
    /// where refresh is deliberately disallowed.
    /// </summary>
    Task<(string AccessToken, DateTime ExpiresAtUtc)> IssueAccessOnlyAsync(
        string subject,
        IEnumerable<Claim> claims,
        CancellationToken ct = default);
}