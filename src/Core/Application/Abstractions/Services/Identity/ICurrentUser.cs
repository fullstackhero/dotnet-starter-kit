using System.Security.Claims;

namespace DN.WebApi.Application.Abstractions.Services.Identity
{
    public interface ICurrentUser : ITransientService
    {
        string Name { get; }
        Guid GetUserId();
        string GetUserEmail();
        string GetTenantId();
        bool IsAuthenticated();
        bool IsInRole(string role);
        IEnumerable<Claim> GetUserClaims();
    }
}