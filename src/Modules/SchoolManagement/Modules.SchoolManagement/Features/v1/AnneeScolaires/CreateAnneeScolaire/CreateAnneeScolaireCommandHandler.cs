using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.CreateAnneeScolaire;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.CreateAnneeScolaire;

public sealed class CreateAnneeScolaireCommandHandler : ICommandHandler<CreateAnneeScolaireCommand, Guid>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateAnneeScolaireCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Guid> Handle(CreateAnneeScolaireCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exists = await _dbContext.AnneeScolaires
            .AnyAsync(a => a.Libelle == command.Libelle, cancellationToken).ConfigureAwait(false);
        if (exists)
            throw new CustomException($"Une année scolaire avec le libellé '{command.Libelle}' existe déjà.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        var annee = AnneeScolaire.Create(command.Libelle, command.DateDebut, command.DateFin, _currentUser.GetUserId().ToString());
        _dbContext.AnneeScolaires.Add(annee);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return annee.Id;
    }
}
