using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.UpdateEcole;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.UpdateEcole;

public sealed class UpdateEcoleCommandHandler : ICommandHandler<UpdateEcoleCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateEcoleCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateEcoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ecole = await _dbContext.Ecoles
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"École avec l'ID '{command.Id}' introuvable.");

        var codeExists = await _dbContext.Ecoles
            .AnyAsync(e => e.CodeEcole == command.CodeEcole && e.Id != command.Id, cancellationToken).ConfigureAwait(false);
        if (codeExists)
            throw new CustomException($"Une école avec le code '{command.CodeEcole}' existe déjà.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        var typeEcole = Enum.Parse<TypeEcole>(command.Type, ignoreCase: true);
        ecole.Update(command.Nom, command.CodeEcole, typeEcole, command.Adresse, command.Telephone, command.Email, command.Region, command.Ville, _currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
