using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.GetMatieres;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.GetMatieres;

public sealed class GetMatieresQueryHandler : IQueryHandler<GetMatieresQuery, IReadOnlyCollection<MatiereDto>>
{
    private readonly SchoolDbContext _dbContext;

    public GetMatieresQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyCollection<MatiereDto>> Handle(GetMatieresQuery query, CancellationToken cancellationToken)
    {
        var q = _dbContext.Matieres.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLowerInvariant();
            q = q.Where(m => m.Nom.ToLower().Contains(search) || m.Code.ToLower().Contains(search));
        }

        return await q
            .OrderBy(m => m.Nom)
            .Select(m => new MatiereDto(m.Id, m.Nom, m.Code, m.Coefficient, m.Description, m.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
