using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.Dimensions;

public static class ImportDimensionsEndpoint
{
    internal static RouteHandlerBuilder MapImportDimensionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Import", async (FileUploadCommand uploadFile, bool isUpdate, ISender mediator) =>
            {
                var response = await mediator.Send(new ImportDimensionsCommand(uploadFile, isUpdate));
                return Results.Ok(response);
             
            })
            .WithName(nameof(ImportDimensionsEndpoint))
            .WithSummary("Imports a list of Dimensions")
            .WithDescription("Imports a list of entities from excel files")
            .Produces<ImportResponse>()
            .RequirePermission("Permissions.Dimensions.Import")
            .MapToApiVersion(1);
    }
}

