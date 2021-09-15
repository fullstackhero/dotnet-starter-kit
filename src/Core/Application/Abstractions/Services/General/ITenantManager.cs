using System.Collections.Generic;
using System.Threading.Tasks;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface ITenantManager : ITransientService
    {
        public Task<Result<TenantDto>> GetByKeyAsync(string key);
        public Task<Result<List<TenantDto>>> GetAllAsync();
        public Task<Result<object>> CreateTenantAsync(CreateTenantRequest request);
    }
}