using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Files.Contracts.Authorization;
using FSH.Modules.Files.Contracts.v1.Queries;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Files.Features.v1.ListTrashedFiles;

public static class ListTrashedFilesEndpoint
{
    internal static RouteHandlerBuilder MapListTrashedFilesEndpoint(this IEndpointRouteBuilder endpoints)
        => endpoints.MapGet("/trash",
                async (int? page, int? pageSize, IMediator mediator, CancellationToken cancellationToken) =>
                    Results.Ok(await mediator.Send(new ListTrashedFilesQuery(page ?? 1, pageSize ?? 50), cancellationToken)))
            .WithName("ListTrashedFiles")
            .WithSummary("List soft-deleted files (admin/trash view)")
            .RequirePermission(FilesPermissions.ViewTrash);
}
