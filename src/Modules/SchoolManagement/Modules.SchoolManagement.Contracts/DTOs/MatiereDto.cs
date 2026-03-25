namespace FSH.Modules.SchoolManagement.Contracts.DTOs;

public sealed record MatiereDto(
    Guid Id,
    string Nom,
    string Code,
    int Coefficient,
    string? Description,
    DateTimeOffset CreatedOnUtc);
