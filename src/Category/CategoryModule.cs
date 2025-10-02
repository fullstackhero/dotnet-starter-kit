using Carter;
using Category.Domain;
using Category.Features.Create.v1;
using Category.Features.Delete.v1;
using Category.Features.Get.v1;
using Category.Features.GetList.v1;
using Category.Features.Update.v1;
using Category.Persistence;
using FSH.Framework.Core.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;  
namespace Category;
 
public static class CategoryModule
{

    public class Endpoints : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var categoryItemGroup = app.MapGroup("categoryItems").WithTags("categoryItems");
            categoryItemGroup.MapCategoryItemCreationEndpoint();
            categoryItemGroup.MapGetCategoryItemEndpoint();
            categoryItemGroup.MapGetCategoryItemListEndpoint();
            categoryItemGroup.MapCategoryItemUpdationEndpoint();
            categoryItemGroup.MapCategoryItemDeletionEndpoint();
        }
    }
    public static WebApplicationBuilder RegisterCategoryItemServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.BindDbContext<CategoryItemDbContext>();
        builder.Services.AddScoped<IDbInitializer, CategoryItemDbInitializer>();
        builder.Services.AddKeyedScoped<IRepository<CategoryItem>, CategoryItemRepository<CategoryItem>>("categoryItem");
        builder.Services.AddKeyedScoped<IReadRepository<CategoryItem>, CategoryItemRepository<CategoryItem>>("categoryItem");
        return builder;
    }
    public static WebApplication UseCategoryItemModule(this WebApplication app)
    {
        return app;
    }
}
