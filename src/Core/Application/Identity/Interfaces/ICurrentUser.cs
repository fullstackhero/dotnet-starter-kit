using System.Security.Claims;
using DN.WebApi.Application.Common.Interfaces;

namespace DN.WebApi.Application.Identity.Interfaces;

public interface ICurrentUser : IScopedService
{
    string Name { get; }

    Guid GetUserId();

    string GetUserEmail();

    string GetTenant();

    bool IsAuthenticated();

    bool IsInRole(string role);

    IEnumerable<Claim> GetUserClaims();

    void SetUser(ClaimsPrincipal user);

    void SetUserJob(string userId);
}