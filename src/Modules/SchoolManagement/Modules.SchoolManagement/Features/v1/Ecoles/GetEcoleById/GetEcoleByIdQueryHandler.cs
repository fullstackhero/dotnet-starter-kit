using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.DTOs;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.GetEcoleById;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.GetEcoleById;

public sealed class GetEcoleByIdQueryHandler : IQueryHandler<GetEcoleByIdQuery, EcoleDto>
{
    private readonly SchoolDbContext _dbContext;

    public GetEcoleByIdQueryHandler(SchoolDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<EcoleDto> Handle(GetEcoleByIdQuery query, CancellationToken cancellationToken)
    {
        var ecole = await _dbContext.Ecoles
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == query.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"École avec l'ID '{query.Id}' introuvable.");

        return new EcoleDto(ecole.Id, ecole.Nom, ecole.CodeEcole, ecole.Type.ToString(), ecole.Adresse, ecole.Telephone, ecole.Email, ecole.Region, ecole.Ville, ecole.CreatedOnUtc);
    }
}
