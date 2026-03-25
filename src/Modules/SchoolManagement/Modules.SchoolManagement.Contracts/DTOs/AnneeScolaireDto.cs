namespace FSH.Modules.SchoolManagement.Contracts.DTOs;

public sealed record AnneeScolaireDto(
    Guid Id,
    string Libelle,
    DateTimeOffset DateDebut,
    DateTimeOffset DateFin,
    bool EstActive,
    DateTimeOffset CreatedOnUtc);
