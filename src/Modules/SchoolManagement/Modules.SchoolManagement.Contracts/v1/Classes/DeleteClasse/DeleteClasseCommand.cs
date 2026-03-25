using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Classes.DeleteClasse;

public sealed record DeleteClasseCommand(Guid Id) : ICommand<Unit>;
