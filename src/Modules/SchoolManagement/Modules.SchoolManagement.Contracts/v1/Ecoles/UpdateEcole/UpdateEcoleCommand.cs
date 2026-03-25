using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.UpdateEcole;

public sealed record UpdateEcoleCommand(
    Guid Id,
    string Nom,
    string CodeEcole,
    string Type,
    string? Adresse,
    string? Telephone,
    string? Email,
    string? Region,
    string? Ville) : ICommand<Unit>;
