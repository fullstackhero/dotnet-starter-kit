using DN.WebApi.Domain.Catalog;
using DN.WebApi.Infrastructure.Identity.Models;
using DN.WebApi.Shared.DTOs.Catalog;
using DN.WebApi.Shared.DTOs.Identity;
using Mapster;

namespace DN.WebApi.Infrastructure.Mapping;

public class MapsterSettings
{
    public static void Configure()
    {
        // here we will define the type conversion / Custom-mapping
        // More details at https://github.com/MapsterMapper/Mapster/wiki/Custom-mapping
        TypeAdapterConfig<ApplicationRoleClaim, PermissionDto>.NewConfig().Map(dest => dest.Permission, src => src.ClaimValue);

        TypeAdapterConfig<Product, ProductDto>.NewConfig().Map(dest => dest.BrandName, src => src.Brand.Name);
    }
}