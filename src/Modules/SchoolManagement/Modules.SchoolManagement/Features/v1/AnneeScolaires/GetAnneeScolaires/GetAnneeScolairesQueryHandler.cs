using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaires;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaires;

public sealed class GetAnneeScolairesQueryHandler : IQueryHandler<GetAnneeScolairesQuery, IReadOnlyCollection<AnneeScolaireDto>>
{
    private readonly SchoolDbContext _dbContext;

    public GetAnneeScolairesQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<IReadOnlyCollection<AnneeScolaireDto>> Handle(GetAnneeScolairesQuery query, CancellationToken cancellationToken)
    {
        return await _dbContext.AnneeScolaires
            .AsNoTracking()
            .OrderByDescending(a => a.DateDebut)
            .Select(a => new AnneeScolaireDto(a.Id, a.Libelle, a.DateDebut, a.DateFin, a.EstActive, a.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
