using Carter;
using FSH.Framework.Infrastructure.Database;
using FSH.WebApi.Todo.Data;
using FSH.WebApi.Todo.Features.Creation.v1;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FSH.WebApi.Todo;
public static class TodoModule
{
    public class Endpoints : CarterModule
    {
        public override void AddRoutes(IEndpointRouteBuilder app)
        {
            var todoGroup = app.MapGroup("todo").WithTags("todo");
            todoGroup.MapTodoItemCreationEndpoint();
        }
    }
    public static WebApplicationBuilder RegisterTodoServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.AddDbContext<TodoDbContext>((p, options) =>
        {
            var dbConfig = p.GetRequiredService<IOptions<DbConfig>>().Value;
            if (dbConfig.UseInMemoryDb)
            {
                options.UseInMemoryDatabase("todo");
            }
        });
        return builder;
    }
    public static WebApplication UseTodoModule(this WebApplication app)
    {
        return app;
    }
}
