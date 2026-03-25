using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Classes.DeleteClasse;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Classes.DeleteClasse;

public sealed class DeleteClasseCommandHandler : ICommandHandler<DeleteClasseCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteClasseCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteClasseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var classe = await _dbContext.Classes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Classe avec l'ID '{command.Id}' introuvable.");

        classe.Delete(_currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
