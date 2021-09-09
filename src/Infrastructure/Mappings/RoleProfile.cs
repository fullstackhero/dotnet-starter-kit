using AutoMapper;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Shared.DTOs.Identity;

namespace DN.WebApi.Infrastructure.Mappings
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<RoleDto, ApplicationRole>().ReverseMap();
        }
    }
}