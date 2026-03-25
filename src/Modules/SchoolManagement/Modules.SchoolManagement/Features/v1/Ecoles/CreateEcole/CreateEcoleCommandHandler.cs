using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.CreateEcole;
using FSH.Modules.SchoolManagement.Domain;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.CreateEcole;

public sealed class CreateEcoleCommandHandler : ICommandHandler<CreateEcoleCommand, Guid>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CreateEcoleCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Guid> Handle(CreateEcoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var exists = await _dbContext.Ecoles
            .AnyAsync(e => e.CodeEcole == command.CodeEcole, cancellationToken).ConfigureAwait(false);
        if (exists)
            throw new CustomException($"A school with code '{command.CodeEcole}' already exists.", (IEnumerable<string>?)null, System.Net.HttpStatusCode.Conflict);

        var typeEcole = Enum.Parse<TypeEcole>(command.Type, ignoreCase: true);
        var ecole = Ecole.Create(command.Nom, command.CodeEcole, typeEcole, command.Adresse, command.Telephone, command.Email, command.Region, command.Ville, _currentUser.GetUserId().ToString());
        _dbContext.Ecoles.Add(ecole);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ecole.Id;
    }
}
