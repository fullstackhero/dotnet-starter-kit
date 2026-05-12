using System.Collections.ObjectModel;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.ListTrashedFiles;

internal sealed class ListTrashedFilesQueryHandler(FilesDbContext db)
    : IQueryHandler<ListTrashedFilesQuery, ReadOnlyCollection<FileAssetDto>>
{
    public async ValueTask<ReadOnlyCollection<FileAssetDto>> Handle(ListTrashedFilesQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 200);

        // IgnoreQueryFilters because the SoftDelete filter would otherwise hide deleted rows —
        // exactly what we DO want here. Tenant scoping is preserved via the per-tenant DbContext.
        var rows = await db.FileAssets
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted)
            .OrderByDescending(f => f.DeletedOnUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(f => FileAssetMapper.ToDto(f)).ToList().AsReadOnly();
    }
}
