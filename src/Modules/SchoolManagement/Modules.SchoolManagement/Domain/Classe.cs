using FSH.Framework.Core.Domain;

namespace FSH.Modules.SchoolManagement.Domain;

public class Classe : IAuditableEntity, ISoftDeletable, IHasTenant
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public NiveauScolaire Niveau { get; private set; }
    public Guid EcoleId { get; private set; }
    public Guid AnneeScolaireId { get; private set; }
    public int Capacite { get; private set; }
    public string TenantId { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public virtual Ecole Ecole { get; private set; } = default!;
    public virtual AnneeScolaire AnneeScolaire { get; private set; } = default!;

    private Classe() { }

    public static Classe Create(string nom, NiveauScolaire niveau, Guid ecoleId, Guid anneeScolaireId, int capacite, string? createdBy = null)
    {
        return new Classe
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Niveau = niveau,
            EcoleId = ecoleId,
            AnneeScolaireId = anneeScolaireId,
            Capacite = capacite,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(string nom, NiveauScolaire niveau, int capacite, string? modifiedBy = null)
    {
        Nom = nom;
        Niveau = niveau;
        Capacite = capacite;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }

    public void Delete(string? deletedBy = null)
    {
        IsDeleted = true;
        DeletedOnUtc = TimeProvider.System.GetUtcNow();
        DeletedBy = deletedBy;
    }
}
