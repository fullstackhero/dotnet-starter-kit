using FSH.Framework.Core.DataIO;
using FSH.Framework.Core.Storage.File.Features;
using FSH.Framework.Infrastructure.Auth.Policy;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Starter.WebApi.Todo.Features.Import.v1;

public static class ImportTodolistEndpoint
{
    internal static RouteHandlerBuilder MapImportTodoListEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapPost("/Import", async (FileUploadCommand uploadFile, bool isUpdate, ISender mediator) =>
            {
                var response = await mediator.Send(new ImportTodoListCommand(uploadFile, isUpdate));
                return Results.Ok(response);
             
            })
            .WithName(nameof(ImportTodolistEndpoint))
            .WithSummary("Imports a list of Todo")
            .WithDescription("Imports a list of entities from excel files")
            .Produces<ImportResponse>()
            .RequirePermission("Permissions.Todos.Import")
            .MapToApiVersion(1);
    }
}

