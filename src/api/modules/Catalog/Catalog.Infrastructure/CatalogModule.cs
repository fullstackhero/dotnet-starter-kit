using Carter;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Starter.WebApi.Catalog.Domain;
using FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;
using FSH.Starter.WebApi.Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Catalog.Infrastructure;
public static class CatalogModule
{
    public class Endpoints : CarterModule
    {
        public Endpoints() : base("catalog") { }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var productGroup = app.MapGroup("products").WithTags("products");
            productGroup.MapProductCreationEndpoint();
            productGroup.MapGetProductEndpoint();
            productGroup.MapGetProductListEndpoint();
            productGroup.MapProductUpdateEndpoint();
            productGroup.MapProductDeleteEndpoint();

            var brandGroup = app.MapGroup("brands").WithTags("brands");
            brandGroup.MapBrandCreationEndpoint();
            brandGroup.MapGetBrandEndpoint();
            brandGroup.MapGetBrandListEndpoint();
            brandGroup.MapBrandUpdateEndpoint();
            brandGroup.MapBrandDeleteEndpoint();

            var AgencyGroup = app.MapGroup("agencies").WithTags("agencies");
            AgencyGroup.MapAgencyCreationEndpoint();
            AgencyGroup.MapGetAgencyEndpoint();
            AgencyGroup.MapGetAgencyListEndpoint();
            AgencyGroup.MapAgencyUpdateEndpoint();
            AgencyGroup.MapAgencyDeleteEndpoint();

            var regionGroup = app.MapGroup("regions").WithTags("regions");
            regionGroup.MapRegionCreationEndpoint();
            regionGroup.MapGetRegionEndpoint();
            regionGroup.MapSearchRegionsEndpoint();
            regionGroup.MapRegionUpdateEndpoint();
            regionGroup.MapRegionDeleteEndpoint();

            var cityGroup = app.MapGroup("cities").WithTags("cities");
            cityGroup.MapCityCreationEndpoint();
            cityGroup.MapGetCityEndpoint();
            cityGroup.MapSearchCitiesEndpoint();
            cityGroup.MapCityUpdateEndpoint();
            cityGroup.MapCityDeleteEndpoint();

            var neighborhoodGroup = app.MapGroup("neighborhoods").WithTags("neighborhoods");
            neighborhoodGroup.MapNeighborhoodCreationEndpoint();
            neighborhoodGroup.MapGetNeighborhoodEndpoint();
            neighborhoodGroup.MapSearchNeighborhoodsEndpoint();
            neighborhoodGroup.MapNeighborhoodUpdateEndpoint();
            neighborhoodGroup.MapNeighborhoodDeleteEndpoint();

            var propertyTypeGroup = app.MapGroup("propertytypes").WithTags("propertytypes");
            propertyTypeGroup.MapPropertyTypeCreationEndpoint();
            propertyTypeGroup.MapGetPropertyTypeEndpoint();
            propertyTypeGroup.MapSearchPropertyTypesEndpoint();
            propertyTypeGroup.MapPropertyTypeUpdateEndpoint();
            propertyTypeGroup.MapPropertyTypeDeleteEndpoint();

            var propertyGroup = app.MapGroup("properties").WithTags("properties");
            propertyGroup.MapPropertyCreationEndpoint();
            propertyGroup.MapGetPropertyEndpoint();
            propertyGroup.MapPropertyUpdateEndpoint();
            propertyGroup.MapSearchPropertiesEndpoint();
            propertyGroup.MapPropertyDeleteEndpoint();

            var reviewGroup = app.MapGroup("reviews").WithTags("reviews");
            reviewGroup.MapReviewCreationEndpoint();
            reviewGroup.MapGetReviewEndpoint();
            reviewGroup.MapSearchReviewsEndpoint();
            reviewGroup.MapReviewUpdateEndpoint();
            reviewGroup.MapReviewDeleteEndpoint();

        
        }
    }
    public static WebApplicationBuilder RegisterCatalogServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.BindDbContext<CatalogDbContext>();
        builder.Services.AddScoped<IDbInitializer, CatalogDbInitializer>();
        builder.Services.AddKeyedScoped<IRepository<Product>, CatalogRepository<Product>>("catalog:products");
        builder.Services.AddKeyedScoped<IReadRepository<Product>, CatalogRepository<Product>>("catalog:products");
        builder.Services.AddKeyedScoped<IRepository<Brand>, CatalogRepository<Brand>>("catalog:brands");
        builder.Services.AddKeyedScoped<IReadRepository<Brand>, CatalogRepository<Brand>>("catalog:brands");
        builder.Services.AddKeyedScoped<IRepository<Agency>, CatalogRepository<Agency>>("catalog:agencies");
        builder.Services.AddKeyedScoped<IReadRepository<Agency>, CatalogRepository<Agency>>("catalog:agencies");
        builder.Services.AddKeyedScoped<IRepository<Region>, CatalogRepository<Region>>("catalog:regions");
        builder.Services.AddKeyedScoped<IReadRepository<Region>, CatalogRepository<Region>>("catalog:regions");
        builder.Services.AddKeyedScoped<IRepository<City>, CatalogRepository<City>>("catalog:cities");
        builder.Services.AddKeyedScoped<IReadRepository<City>, CatalogRepository<City>>("catalog:cities");
        builder.Services.AddKeyedScoped<IRepository<Neighborhood>, CatalogRepository<Neighborhood>>("catalog:neighborhoods");
        builder.Services.AddKeyedScoped<IReadRepository<Neighborhood>, CatalogRepository<Neighborhood>>("catalog:neighborhoods");
        builder.Services.AddKeyedScoped<IRepository<PropertyType>, CatalogRepository<PropertyType>>("catalog:propertytypes");
        builder.Services.AddKeyedScoped<IReadRepository<PropertyType>, CatalogRepository<PropertyType>>("catalog:propertytypes");
        builder.Services.AddKeyedScoped<IRepository<Property>, CatalogRepository<Property>>("catalog:properties");
        builder.Services.AddKeyedScoped<IReadRepository<Property>, CatalogRepository<Property>>("catalog:properties");
        builder.Services.AddKeyedScoped<IRepository<Review>, CatalogRepository<Review>>("catalog:reviews");
        builder.Services.AddKeyedScoped<IReadRepository<Review>, CatalogRepository<Review>>("catalog:reviews");
        return builder;
    }
    public static WebApplication UseCatalogModule(this WebApplication app)
    {
        return app;
    }
}
