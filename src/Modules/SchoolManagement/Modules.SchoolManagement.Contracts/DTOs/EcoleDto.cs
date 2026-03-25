namespace FSH.Modules.SchoolManagement.Contracts.DTOs;

public sealed record EcoleDto(
    Guid Id,
    string Nom,
    string CodeEcole,
    string Type,
    string? Adresse,
    string? Telephone,
    string? Email,
    string? Region,
    string? Ville,
    DateTimeOffset CreatedOnUtc);
