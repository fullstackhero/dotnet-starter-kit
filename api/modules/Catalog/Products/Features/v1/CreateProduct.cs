using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace FSH.WebApi.Modules.Catalog.Products.Features.v1;

public static class CreateProduct
{
    public sealed record Command(string Name, Guid CategoryId, decimal Price, string? Description = null);
    public static class Handler
    {
        public static void Handle(Command command, ILogger logger)
        {
            logger.LogInformation("product created");
        }
    }
    public static RouteHandlerBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/",
            (Command command, IMessageBus bus) => bus.InvokeAsync(command)
        )
        .WithName(nameof(CreateProduct))
        .WithDisplayName(nameof(CreateProduct))
        .WithTags("products");
    }
}
