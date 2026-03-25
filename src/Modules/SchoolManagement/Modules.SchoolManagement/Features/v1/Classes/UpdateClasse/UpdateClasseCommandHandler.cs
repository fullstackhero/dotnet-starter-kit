using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.UpdateClasse;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.UpdateClasse;

public sealed class UpdateClasseCommandHandler : ICommandHandler<UpdateClasseCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateClasseCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateClasseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var classe = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Classe avec l'ID '{command.Id}' introuvable.");

        var niveau = Enum.Parse<NiveauScolaire>(command.Niveau, ignoreCase: true);
        classe.Update(command.Nom, niveau, command.Capacite, _currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
