using Carter;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.Starter.WebApi.Setting.Domain;
using FSH.Starter.WebApi.Setting.Features.v1.Dimensions;
using FSH.Starter.WebApi.Setting.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Starter.WebApi.Setting;
public static class DimensionModule
{

    public class Endpoints : CarterModule
    {
        public Endpoints() : base("setting") { }
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var settingGroup = app.MapGroup("Dimensions").WithTags("Dimensions");
            settingGroup.MapDimensionCreationEndpoint();
            settingGroup.MapGetDimensionEndpoint();
            settingGroup.MapGetDimensionListEndpoint();
            settingGroup.MapDimensionUpdationEndpoint();
            settingGroup.MapDimensionDeletionEndpoint();
        }
    }
    public static WebApplicationBuilder RegisterDimensionServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.BindDbContext<DimensionDbContext>();
        builder.Services.AddScoped<IDbInitializer, DimensionDbInitializer>();
        builder.Services.AddKeyedScoped<IRepository<Dimension>, DimensionRepository<Dimension>>("setting:dimension");
        builder.Services.AddKeyedScoped<IReadRepository<Dimension>, DimensionRepository<Dimension>>("setting:dimension");
        return builder;
    }
    public static WebApplication UseDimensionModule(this WebApplication app)
    {
        return app;
    }
}
