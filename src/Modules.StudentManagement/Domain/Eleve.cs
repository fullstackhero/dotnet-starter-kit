using FSH.Framework.Core.Domain;


namespace FSH.Modules.StudentManagement.Domain;

public class Eleve : IAuditableEntity, ISoftDeletable, IHasTenant
{
    public Guid Id { get; private set; }
    public string Matricule { get; private set; } = default!;
    public string Nom { get; private set; } = default!;
    public string Prenom { get; private set; } = default!;
    public DateOnly DateNaissance { get; private set; }
    public string? LieuNaissance { get; private set; }
    public Sexe Sexe { get; private set; }
    public StatutEleve Statut { get; private set; }
    public string? Adresse { get; private set; }
    public string? Telephone { get; private set; }
    public string? Email { get; private set; }
    public string? NomParent { get; private set; }
    public string? TelephoneParent { get; private set; }

    // Tenant (multi-école)
    public string TenantId { get; private set; } = default!;

    // Audit
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    // Soft delete
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Eleve() { }

    public static Eleve Create(
        string matricule,
        string nom,
        string prenom,
        DateOnly dateNaissance,
        Sexe sexe,
        StatutEleve statut = StatutEleve.Actif,
        string? lieuNaissance = null,
        string? adresse = null,
        string? telephone = null,
        string? email = null,
        string? nomParent = null,
        string? telephoneParent = null,
        string? createdBy = null)
    {
        return new Eleve
        {
            Id = Guid.NewGuid(),
            Matricule = matricule,
            Nom = nom,
            Prenom = prenom,
            DateNaissance = dateNaissance,
            Sexe = sexe,
            Statut = statut,
            LieuNaissance = lieuNaissance,
            Adresse = adresse,
            Telephone = telephone,
            Email = email,
            NomParent = nomParent,
            TelephoneParent = telephoneParent,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(
        string nom,
        string prenom,
        DateOnly dateNaissance,
        Sexe sexe,
        StatutEleve statut,
        string? lieuNaissance = null,
        string? adresse = null,
        string? telephone = null,
        string? email = null,
        string? nomParent = null,
        string? telephoneParent = null,
        string? modifiedBy = null)
    {
        Nom = nom;
        Prenom = prenom;
        DateNaissance = dateNaissance;
        Sexe = sexe;
        Statut = statut;
        LieuNaissance = lieuNaissance;
        Adresse = adresse;
        Telephone = telephone;
        Email = email;
        NomParent = nomParent;
        TelephoneParent = telephoneParent;
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