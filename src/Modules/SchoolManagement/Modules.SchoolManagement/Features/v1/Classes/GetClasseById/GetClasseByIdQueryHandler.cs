using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.GetClasseById;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.GetClasseById;

public sealed class GetClasseByIdQueryHandler : IQueryHandler<GetClasseByIdQuery, ClasseDto>
{
    private readonly SchoolDbContext _dbContext;

    public GetClasseByIdQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<ClasseDto> Handle(GetClasseByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var classe = await _dbContext.Classes
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Class with ID '{query.Id}' not found.");

        return new ClasseDto(classe.Id, classe.Nom, classe.Niveau.ToString(), classe.EcoleId, classe.AnneeScolaireId, classe.Capacite, classe.CreatedOnUtc);
    }
}
