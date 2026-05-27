using FSH.Framework.Shared.Persistence;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Contracts.v1.Queries;
using FSH.Modules.Files.Data;
using FSH.Modules.Files.Features.v1.Internal;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Files.Features.v1.ListTrashedFiles;

public sealed class ListTrashedFilesQueryHandler(FilesDbContext db)
    : IQueryHandler<ListTrashedFilesQuery, PagedResponse<FileAssetDto>>
{
    public async ValueTask<PagedResponse<FileAssetDto>> Handle(ListTrashedFilesQuery q, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(q);

        int page = q.PageNumber < 1 ? 1 : q.PageNumber;
        int size = q.PageSize is < 1 or > 200 ? 20 : q.PageSize;

        // IgnoreQueryFilters because the SoftDelete filter would otherwise hide deleted rows —
        // exactly what we DO want here. Tenant scoping is preserved via the per-tenant DbContext.
        var baseQuery = db.FileAssets
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted)
            .OrderByDescending(f => f.DeletedOnUtc);

        long total = await baseQuery.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var rows = await baseQuery
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<FileAssetDto>
        {
            Items = rows.Select(f => FileAssetMapper.ToDto(f)).ToList(),
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size),
        };
    }
}
