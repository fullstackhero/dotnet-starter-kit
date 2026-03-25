using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoles;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoles;

public sealed class GetEcolesQueryHandler : IQueryHandler<GetEcolesQuery, PagedResponse<EcoleDto>>
{
    private readonly SchoolDbContext _dbContext;

    public GetEcolesQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<EcoleDto>> Handle(GetEcolesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = _dbContext.Ecoles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search;
            q = q.Where(e => e.Nom.Contains(search) || e.CodeEcole.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Region))
            q = q.Where(e => e.Region == query.Region);

        if (!string.IsNullOrWhiteSpace(query.Type) && Enum.TryParse<TypeEcole>(query.Type, ignoreCase: true, out var typeEcole))
            q = q.Where(e => e.Type == typeEcole);

        var totalCount = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        int pageNumber = Math.Max(query.PageNumber ?? 1, 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var items = await q
            .OrderBy(e => e.Nom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EcoleDto(e.Id, e.Nom, e.CodeEcole, e.Type.ToString(), e.Adresse, e.Telephone, e.Email, e.Region, e.Ville, e.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResponse<EcoleDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
