using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.AnneeScolaires.UpdateAnneeScolaire;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.AnneeScolaires.UpdateAnneeScolaire;

public sealed class UpdateAnneeScolaireCommandHandler : ICommandHandler<UpdateAnneeScolaireCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateAnneeScolaireCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateAnneeScolaireCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var annee = await _dbContext.AnneeScolaires
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Année scolaire avec l'ID '{command.Id}' introuvable.");

        var libelleExists = await _dbContext.AnneeScolaires
            .AnyAsync(a => a.Libelle == command.Libelle && a.Id != command.Id, cancellationToken).ConfigureAwait(false);
        if (libelleExists)
            throw new CustomException($"Une année scolaire avec le libellé '{command.Libelle}' existe déjà.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        annee.Update(command.Libelle, command.DateDebut, command.DateFin, command.EstActive, _currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
