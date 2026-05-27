using FSH.Modules.Files.Contracts.v1.Commands;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.FinalizeUpload;

public static class FinalizeUploadEndpoint
{
    internal static RouteHandlerBuilder MapFinalizeUploadEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapPost("/{id:guid}/finalize",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new FinalizeUploadCommand(id), cancellationToken)))
            .WithName("FinalizeFileUpload")
            .WithSummary("Finalize a file upload after the browser PUT completes")
            .RequireAuthorization();
}
