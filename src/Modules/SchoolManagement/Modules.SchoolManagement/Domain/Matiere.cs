using FSH.Framework.Core.Domain;

namespace FSH.Modules.SchoolManagement.Domain;

public class Matiere : IAuditableEntity, IHasTenant
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public int Coefficient { get; private set; }
    public string? Description { get; private set; }
    public string TenantId { get; private set; } = default!;

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private Matiere() { }

    public static Matiere Create(string nom, string code, int coefficient, string? description = null, string? createdBy = null)
    {
        return new Matiere
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Code = code,
            Coefficient = coefficient,
            Description = description,
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy
        };
    }

    public void Update(string nom, string code, int coefficient, string? description, string? modifiedBy = null)
    {
        Nom = nom;
        Code = code;
        Coefficient = coefficient;
        Description = description;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }
}
