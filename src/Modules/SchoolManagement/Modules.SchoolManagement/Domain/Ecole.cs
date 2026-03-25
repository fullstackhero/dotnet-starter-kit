using FSH.Framework.Core.Domain;

namespace FSH.Modules.SchoolManagement.Domain;

public class Ecole : IAuditableEntity, ISoftDeletable, IHasTenant
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public string? Adresse { get; private set; }
    public string? Telephone { get; private set; }
    public string? Email { get; private set; }
    public TypeEcole Type { get; private set; }
    public string? Region { get; private set; }
    public string? Ville { get; private set; }
    public string CodeEcole { get; private set; } = default!;
    public string TenantId { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Ecole() { }

    public static Ecole Create(string nom, string codeEcole, TypeEcole type, string? adresse = null, string? telephone = null, string? email = null, string? region = null, string? ville = null, string? createdBy = null)
    {
        return new Ecole
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            CodeEcole = codeEcole,
            Type = type,
            Adresse = adresse,
            Telephone = telephone,
            Email = email,
            Region = region,
            Ville = ville,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(string nom, string codeEcole, TypeEcole type, string? adresse, string? telephone, string? email, string? region, string? ville, string? modifiedBy = null)
    {
        Nom = nom;
        CodeEcole = codeEcole;
        Type = type;
        Adresse = adresse;
        Telephone = telephone;
        Email = email;
        Region = region;
        Ville = ville;
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
