using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Matieres.CreateMatiere;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Matieres.CreateMatiere;

public sealed class CreateMatiereCommandHandler : ICommandHandler<CreateMatiereCommand, Guid>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateMatiereCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Guid> Handle(CreateMatiereCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exists = await _dbContext.Matieres
            .AnyAsync(m => m.Code == command.Code, cancellationToken).ConfigureAwait(false);
        if (exists)
            throw new CustomException($"A subject with code '{command.Code}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        var matiere = Matiere.Create(command.Nom, command.Code, command.Coefficient, command.Description, _currentUser.GetUserId().ToString());
        _dbContext.Matieres.Add(matiere);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return matiere.Id;
    }
}
