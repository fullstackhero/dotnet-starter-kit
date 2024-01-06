using Carter;
using FSH.Framework.Core.Abstraction.Persistence;
using FSH.Framework.Infrastructure.Persistence;
using FSH.WebApi.Todo.Features.Creation.v1;
using FSH.WebApi.Todo.Features.Get.v1;
using FSH.WebApi.Todo.Features.GetList.v1;
using FSH.WebApi.Todo.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.WebApi.Todo;
public static class TodoModule
{
    public class Endpoints : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var todoGroup = app.MapGroup("todo").WithTags("todo");
            todoGroup.MapTodoItemCreationEndpoint();
            todoGroup.MapGetTodoEndpoint();
            todoGroup.MapGetTodoListEndpoint();
        }
    }
    public static WebApplicationBuilder RegisterTodoServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.BindDbContext<TodoDbContext>();
        builder.Services.AddScoped<IDbInitializer, TodoDbInitializer>();
        builder.Services.AddScoped(typeof(IRepository<>), typeof(TodoRepository<>));
        builder.Services.AddScoped(typeof(IReadRepository<>), typeof(TodoRepository<>));
        return builder;
    }
    public static WebApplication UseTodoModule(this WebApplication app)
    {
        return app;
    }
}
