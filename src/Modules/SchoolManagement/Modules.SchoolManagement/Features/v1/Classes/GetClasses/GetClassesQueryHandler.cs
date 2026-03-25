using FSH.Framework.Shared.Persistence;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasses;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasses;

public sealed class GetClassesQueryHandler : IQueryHandler<GetClassesQuery, PagedResponse<ClasseDto>>
{
    private readonly SchoolDbContext _dbContext;

    public GetClassesQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<PagedResponse<ClasseDto>> Handle(GetClassesQuery query, CancellationToken cancellationToken)
    {
        var q = _dbContext.Classes.AsNoTracking().AsQueryable();

        if (query.EcoleId.HasValue)
            q = q.Where(c => c.EcoleId == query.EcoleId.Value);

        if (query.AnneeScolaireId.HasValue)
            q = q.Where(c => c.AnneeScolaireId == query.AnneeScolaireId.Value);

        if (!string.IsNullOrWhiteSpace(query.Niveau) && Enum.TryParse<NiveauScolaire>(query.Niveau, ignoreCase: true, out var niveau))
            q = q.Where(c => c.Niveau == niveau);

        var totalCount = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        int pageNumber = Math.Max(query.PageNumber ?? 1, 1);
        int pageSize = Math.Clamp(query.PageSize ?? 20, 1, 100);

        var items = await q
            .OrderBy(c => c.Nom)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ClasseDto(c.Id, c.Nom, c.Niveau.ToString(), c.EcoleId, c.AnneeScolaireId, c.Capacite, c.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResponse<ClasseDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
