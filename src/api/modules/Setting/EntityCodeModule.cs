// using Carter;
// using FSH.Framework.Core.Persistence;
// using FSH.Framework.Infrastructure.Persistence;
// using FSH.Starter.WebApi.Setting.Domain;
// using FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;
// using FSH.Starter.WebApi.Setting.Persistence;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Routing;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace FSH.Starter.WebApi.Setting;
// public static class EntityCodeModule
// {
//
//     public class Endpoints : CarterModule
//     {
//         public Endpoints() : base("setting") { }
//         public override void AddRoutes(IEndpointRouteBuilder app)
//         {
//             var settingGroup = app.MapGroup("EntityCodes").WithTags("EntityCodes");
//             settingGroup.MapEntityCodeCreationEndpoint();
//             settingGroup.MapGetEntityCodeEndpoint();
//             settingGroup.MapGetEntityCodeListEndpoint();
//             settingGroup.MapEntityCodeUpdationEndpoint();
//             settingGroup.MapEntityCodeDeletionEndpoint();
//         }
//     }
//     public static WebApplicationBuilder RegisterEntityCodeServices(this WebApplicationBuilder builder)
//     {
//         ArgumentNullException.ThrowIfNull(builder);
//         builder.Services.BindDbContext<SettingDbContext>();
//         builder.Services.AddScoped<IDbInitializer, SettingDbInitializer>();
//         builder.Services.AddKeyedScoped<IRepository<EntityCode>, SettingRepository<EntityCode>>("setting:EntityCode");
//         builder.Services.AddKeyedScoped<IReadRepository<EntityCode>, SettingRepository<EntityCode>>("setting:EntityCode");
//         return builder;
//     }
//     public static WebApplication UseEntityCodeModule(this WebApplication app)
//     {
//         return app;
//     }
// }
