using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Files.Contracts.Authorization;
using FSH.Modules.Files.Contracts.v1.Commands;
using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.ChangeVisibility;

public static class ChangeFileVisibilityEndpoint
{
    internal static RouteHandlerBuilder MapChangeFileVisibilityEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPatch("/{id:guid}/visibility",
                async (Guid id, ChangeVisibilityRequest body, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var dto = await mediator.Send(new ChangeFileVisibilityCommand(id, body.Visibility), cancellationToken);
                    return Results.Ok(dto);
                })
            .WithName("ChangeFileVisibility")
            .WithSummary("Flip a file's visibility (Public ↔ Private)")
            .WithDescription("Authenticated upload permission gates the HTTP surface; per-file authorization is delegated to the OwnerType's IFileAccessPolicy (uploader-only by default).")
            // Upload is a basic permission so every authenticated user has it; the per-file
            // policy check inside the handler refines who can actually flip the bit.
            .RequirePermission(FilesPermissions.Upload)
            .Produces<FileAssetDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound);
}

public sealed record ChangeVisibilityRequest(int Visibility);
