using AutoMapper;
using DN.WebApi.Domain.Entities.Multitenancy;
using DN.WebApi.Shared.DTOs.Multitenancy;

namespace DN.WebApi.Infrastructure.Mappings
{
    public class TenantProfile : Profile
    {
        public TenantProfile()
        {
            CreateMap<TenantDto, Tenant>().ReverseMap();
            CreateMap<CreateTenantRequest, Tenant>();
        }
    }
}