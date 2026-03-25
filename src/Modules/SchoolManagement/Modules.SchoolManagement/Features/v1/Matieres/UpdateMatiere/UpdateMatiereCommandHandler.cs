using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.UpdateMatiere;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.UpdateMatiere;

public sealed class UpdateMatiereCommandHandler : ICommandHandler<UpdateMatiereCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public UpdateMatiereCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(UpdateMatiereCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var matiere = await _dbContext.Matieres
            .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Subject with ID '{command.Id}' not found.");

        var codeExists = await _dbContext.Matieres
            .AnyAsync(m => m.Code == command.Code && m.Id != command.Id, cancellationToken).ConfigureAwait(false);
        if (codeExists)
            throw new CustomException($"A subject with code '{command.Code}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        matiere.Update(command.Nom, command.Code, command.Coefficient, command.Description, _currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
