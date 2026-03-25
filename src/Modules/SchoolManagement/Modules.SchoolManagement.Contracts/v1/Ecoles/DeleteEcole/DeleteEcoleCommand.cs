using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.DeleteEcole;

public sealed record DeleteEcoleCommand(Guid Id) : ICommand<Unit>;
