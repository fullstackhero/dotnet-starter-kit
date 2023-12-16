using System.Collections.ObjectModel;
using FluentValidation;
using FSH.WebApi.Modules.Catalog.Products.Models;
using Mapster;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace FSH.WebApi.Modules.Catalog.Products.Features.v1;

public static class CreateProduct
{
    public sealed record Command(string? Name, decimal Price, Collection<string> Tags, string? Description = null);
    public static class Handler
    {
        public static void Handle(Command command, ILogger logger)
        {
            ArgumentNullException.ThrowIfNull(command);
            var product = command.Adapt<Product>();
            logger.LogInformation("product created {ProductId}", product.Id);
        }
    }
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(p => p.Name).NotEmpty().MinimumLength(10).MaximumLength(75);
        }
    }
    public static RouteHandlerBuilder MapCreateProductEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost(
            "/", (Command command, IMessageBus bus) =>
            {
                bus.InvokeAsync(command);
                return Results.Created();
            }
        )
        .WithName(nameof(CreateProduct))
        .Produces(StatusCodes.Status204NoContent)
        .WithTags("products")
        .MapToApiVersion(1.0);
    }
}
