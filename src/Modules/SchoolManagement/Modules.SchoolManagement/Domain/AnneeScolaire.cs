using FSH.Framework.Core.Domain;

namespace FSH.Modules.SchoolManagement.Domain;

public class AnneeScolaire : IAuditableEntity, IHasTenant
{
    public Guid Id { get; private set; }
    public string Libelle { get; private set; } = default!;
    public DateTimeOffset DateDebut { get; private set; }
    public DateTimeOffset DateFin { get; private set; }
    public bool EstActive { get; private set; }
    public string TenantId { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private AnneeScolaire() { }

    public static AnneeScolaire Create(string libelle, DateTimeOffset dateDebut, DateTimeOffset dateFin, string? createdBy = null)
    {
        return new AnneeScolaire
        {
            Id = Guid.NewGuid(),
            Libelle = libelle,
            DateDebut = dateDebut,
            DateFin = dateFin,
            EstActive = false,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(string libelle, DateTimeOffset dateDebut, DateTimeOffset dateFin, bool estActive, string? modifiedBy = null)
    {
        Libelle = libelle;
        DateDebut = dateDebut;
        DateFin = dateFin;
        EstActive = estActive;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }
}
