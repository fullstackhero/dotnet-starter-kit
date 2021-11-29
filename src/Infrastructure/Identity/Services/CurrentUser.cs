using DN.WebApi.Infrastructure.Identity.Extensions;
using System.Security.Claims;
using DN.WebApi.Application.Identity.Interfaces;

namespace DN.WebApi.Infrastructure.Identity.Services;

public class CurrentUser : ICurrentUser
{
    private ClaimsPrincipal _user;

    public string Name => _user?.Identity?.Name;

    private Guid _userId = Guid.Empty;

    public Guid GetUserId()
    {
        if (IsAuthenticated())
        {
            return Guid.Parse(_user?.GetUserId() ?? Guid.Empty.ToString());
        }

        return _userId;
    }

    public string GetUserEmail()
    {
        return IsAuthenticated() ? _user?.GetUserEmail() : string.Empty;
    }

    public bool IsAuthenticated()
    {
        return _user?.Identity?.IsAuthenticated ?? false;
    }

    public bool IsInRole(string role)
    {
        return _user.IsInRole(role);
    }

    public IEnumerable<Claim> GetUserClaims()
    {
        return _user?.Claims;
    }

    public string GetTenant()
    {
        if (IsAuthenticated())
        {
            return _user?.GetTenant();
        }

        return string.Empty;
    }

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