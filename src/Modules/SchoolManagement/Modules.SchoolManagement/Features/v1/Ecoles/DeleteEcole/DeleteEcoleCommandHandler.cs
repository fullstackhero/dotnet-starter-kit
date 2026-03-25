using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.DeleteEcole;
using FSH.Modules.SchoolManagement.Persistence;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SchoolManagement.Features.v1.Ecoles.DeleteEcole;

public sealed class DeleteEcoleCommandHandler : ICommandHandler<DeleteEcoleCommand, Unit>
{
    private readonly SchoolDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public DeleteEcoleCommandHandler(SchoolDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteEcoleCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var ecole = await _dbContext.Ecoles
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"School with ID '{command.Id}' not found.");

        ecole.Delete(_currentUser.GetUserId().ToString());
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
