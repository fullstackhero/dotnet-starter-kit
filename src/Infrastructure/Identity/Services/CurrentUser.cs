using System.Security.Claims;
using DN.WebApi.Application.Identity.Interfaces;
using DN.WebApi.Infrastructure.Identity.Extensions;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class CurrentUser : ICurrentUser
{
    private ClaimsPrincipal? _user;

    public string? Name => _user?.Identity?.Name;

    private Guid _userId = Guid.Empty;

    public Guid GetUserId() =>
        IsAuthenticated() ? Guid.Parse(_user?.GetUserId() ?? Guid.Empty.ToString()) : _userId;

    public string? GetUserEmail() =>
        IsAuthenticated() ? _user?.GetUserEmail() : string.Empty;

    public bool IsAuthenticated() =>
        _user?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        _user?.IsInRole(role) ?? false;

    public IEnumerable<Claim>? GetUserClaims() =>
        _user?.Claims;

    public string? GetTenant() =>
        IsAuthenticated() ? _user?.GetTenant() : string.Empty;

    public void SetUser(ClaimsPrincipal user)
    {
        if (_user != null)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        _user = user;
    }

    public void SetUserJob(string userId)
    {
        if (_userId != Guid.Empty)
        {
            throw new Exception("Method reserved for in-scope initialization");
        }

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = Guid.Parse(userId);
        }
    }
}