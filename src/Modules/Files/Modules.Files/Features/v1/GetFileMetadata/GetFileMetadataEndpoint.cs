using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.GetFileMetadata;

public static class GetFileMetadataEndpoint
{
    internal static RouteHandlerBuilder MapGetFileMetadataEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new GetFileMetadataQuery(id), cancellationToken)))
            .WithName("GetFileMetadata")
            .WithSummary("Get FileAsset metadata (plus a public URL if Visibility=Public)")
            .RequireAuthorization();
}
