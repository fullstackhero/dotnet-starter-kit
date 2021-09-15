using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.Identity
{
    public interface IPermissionService : ITransientService
    {
        public Task<bool> HasPermissionAsync(string userId, string permission);
    }
}