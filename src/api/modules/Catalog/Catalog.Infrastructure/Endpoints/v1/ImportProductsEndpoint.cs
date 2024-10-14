using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using FSH.Framework.Infrastructure.Auth.Policy;
using FSH.Starter.WebApi.Catalog.Application.Products.Import.v1;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Catalog.Infrastructure.Endpoints.v1;

public static class ImportProductsEndpoint
{
    internal static RouteHandlerBuilder MapImportProductsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Import", async (FileUploadCommand uploadFile, bool isUpdate, ISender mediator) =>
            {
                var response = await mediator.Send(new ImportProductsCommand(uploadFile, isUpdate));
                return Results.Ok(response);
             
            })
            .WithName(nameof(ImportProductsEndpoint))
            .WithSummary("Imports a list of products")
            .WithDescription("Imports a list of entities from excel files")
            .Produces<ImportResponse>()
            .RequirePermission("Permissions.Products.Import")
            .MapToApiVersion(1);
    }
}

