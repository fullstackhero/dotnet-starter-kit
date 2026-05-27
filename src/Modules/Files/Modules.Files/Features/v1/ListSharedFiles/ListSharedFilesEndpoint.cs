using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Files.Contracts.Authorization;
using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.ListSharedFiles;

public static class ListSharedFilesEndpoint
{
    internal static RouteHandlerBuilder MapListSharedFilesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/shared",
                async (int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new ListSharedFilesQuery(page ?? 1, pageSize ?? 20), cancellationToken)))
            .WithName("ListSharedFiles")
            .WithSummary("List Public files in this tenant (the 'Shared' view)")
            .RequirePermission(FilesPermissions.Upload);
}
