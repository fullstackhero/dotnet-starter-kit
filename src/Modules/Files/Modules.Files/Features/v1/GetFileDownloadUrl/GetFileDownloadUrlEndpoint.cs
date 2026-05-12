using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.GetFileDownloadUrl;

public static class GetFileDownloadUrlEndpoint
{
    internal static RouteHandlerBuilder MapGetFileDownloadUrlEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/{id:guid}/url",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new GetFileDownloadUrlQuery(id), cancellationToken)))
            .WithName("GetFileDownloadUrl")
            .WithSummary("Mint a short-lived presigned download URL")
            .RequireAuthorization();
}
