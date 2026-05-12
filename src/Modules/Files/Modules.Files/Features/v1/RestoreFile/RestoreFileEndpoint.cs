using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Files.Contracts.Authorization;
using FSH.Modules.Files.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.RestoreFile;

public static class RestoreFileEndpoint
{
    internal static RouteHandlerBuilder MapRestoreFileEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new RestoreFileCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("RestoreFile")
            .WithSummary("Restore a soft-deleted file from trash (admin)")
            .RequirePermission(FilesPermissions.Restore);
}
