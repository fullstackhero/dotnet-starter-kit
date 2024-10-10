using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Setting.Features.v1.EntityCodes;

public static class ImportEntityCodesEndpoint
{
    internal static RouteHandlerBuilder MapImportEntityCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Import", async (FileUploadCommand uploadFile, bool isUpdate, ISender mediator) =>
            {
                var response = await mediator.Send(new EntityCodes.ImportEntityCodesCommand(uploadFile, isUpdate));
                return Results.Ok(response);
             
            })
            .WithName(nameof(ImportEntityCodesEndpoint))
            .WithSummary("Imports a list of EntityCodes")
            .WithDescription("Imports a list of entities from excel files")
            .Produces<ImportResponse>()
            .RequirePermission("Permissions.EntityCodes.Import")
            .MapToApiVersion(1);
    }
}

