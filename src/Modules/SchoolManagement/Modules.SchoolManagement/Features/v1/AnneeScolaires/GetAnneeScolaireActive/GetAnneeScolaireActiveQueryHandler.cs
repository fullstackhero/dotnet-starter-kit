using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.GetAnneeScolaireActive;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.GetAnneeScolaireActive;

public sealed class GetAnneeScolaireActiveQueryHandler : IQueryHandler<GetAnneeScolaireActiveQuery, AnneeScolaireDto?>
{
    private readonly SchoolDbContext _dbContext;

    public GetAnneeScolaireActiveQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AnneeScolaireDto?> Handle(GetAnneeScolaireActiveQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var annee = await _dbContext.AnneeScolaires
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EstActive, cancellationToken).ConfigureAwait(false);

        return annee is null
            ? null
            : new AnneeScolaireDto(annee.Id, annee.Libelle, annee.DateDebut, annee.DateFin, annee.EstActive, annee.CreatedOnUtc);
    }
}
