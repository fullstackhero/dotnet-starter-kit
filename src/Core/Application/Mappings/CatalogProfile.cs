using AutoMapper;
using DN.WebApi.Domain.Entities.Catalog;
using DN.WebApi.Shared.DTOs.Catalog;

namespace DN.WebApi.Application.Mappings
{
    public class CatalogProfile : Profile
    {
        public CatalogProfile()
        {
            CreateMap<ProductDetailsDto, Product>().ReverseMap();
        }
    }
}