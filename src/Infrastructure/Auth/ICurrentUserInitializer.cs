using System.Security.Claims;

namespace FL_CRMS_ERP_WEBAPI.Infrastructure.Auth;

public interface ICurrentUserInitializer
{
    void SetCurrentUser(ClaimsPrincipal user);

    void SetCurrentUserId(string userId);
}