namespace FSH.Modules.SchoolManagement.Contracts.DTOs;

public sealed record ClasseDto(
    Guid Id,
    string Nom,
    string Niveau,
    Guid EcoleId,
    Guid AnneeScolaireId,
    int Capacite,
    DateTimeOffset CreatedOnUtc);
