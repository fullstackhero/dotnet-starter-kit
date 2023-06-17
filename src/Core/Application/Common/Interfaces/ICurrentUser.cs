using System.Security.Claims;

namespace FL_CRMS_ERP_WEBAPI.Application.Common.Interfaces;

public interface ICurrentUser
{
    string? Name { get; }

    Guid GetUserId();

    string? GetUserEmail();

    string? GetTenant();

    bool IsAuthenticated();

    bool IsInRole(string role);

    IEnumerable<Claim>? GetUserClaims();
}