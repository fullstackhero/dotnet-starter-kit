using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.CreateClasse;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.CreateClasse;

public sealed class CreateClasseCommandHandler : ICommandHandler<CreateClasseCommand, Guid>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateClasseCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Guid> Handle(CreateClasseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ecoleExists = await _dbContext.Ecoles
            .AnyAsync(e => e.Id == command.EcoleId, cancellationToken).ConfigureAwait(false);
        if (!ecoleExists)
            throw new NotFoundException($"École avec l'ID '{command.EcoleId}' introuvable.");

        var anneeExists = await _dbContext.AnneeScolaires
            .AnyAsync(a => a.Id == command.AnneeScolaireId, cancellationToken).ConfigureAwait(false);
        if (!anneeExists)
            throw new NotFoundException($"Année scolaire avec l'ID '{command.AnneeScolaireId}' introuvable.");

        var niveau = Enum.Parse<NiveauScolaire>(command.Niveau, ignoreCase: true);
        var classe = Classe.Create(command.Nom, niveau, command.EcoleId, command.AnneeScolaireId, command.Capacite, _currentUser.GetUserId().ToString());
        _dbContext.Classes.Add(classe);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return classe.Id;
    }
}
