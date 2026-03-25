using Mediator;

namespace FSH.Modules.SchoolManagement.Contracts.v1.Ecoles.CreateEcole;

public sealed record CreateEcoleCommand(
    string Nom,
    string CodeEcole,
    string Type,
    string? Adresse,
    string? Telephone,
    string? Email,
    string? Region,
    string? Ville) : ICommand<Guid>;
