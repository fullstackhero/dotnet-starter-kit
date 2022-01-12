using FSH.WebApi.Shared.Multitenancy;

namespace FSH.WebApi.Domain.Multitenancy;

public class Tenant : AuditableEntity, IAggregateRoot
{
    public string? Name { get; private set; }
    public string? Key { get; private set; }
    public string? AdminEmail { get; private set; }
    public string? ConnectionString { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime ValidUpto { get; private set; }

    public string? Issuer { get; set; }

    public Tenant(string? name, string? key, string? adminEmail, string? connectionString)
    {
        Name = name;
        Key = key;
        AdminEmail = adminEmail;
        ConnectionString = connectionString;
        IsActive = true;

        // Add Default 1 Month Validity for all new tenants. Something like a DEMO period for tenants.
        ValidUpto = DateTime.UtcNow.AddMonths(1);
    }

    public Tenant()
    {
    }

    public void AddValidity(int months)
    {
        ValidUpto = ValidUpto.AddMonths(months);
    }

    public void SetValidity(in DateTime validTill)
    {
        if (ValidUpto < validTill)
            ValidUpto = validTill;
        else
            throw new Exception("Subscription cannot be backdated.");
    }

    public void Activate()
    {
        if (Key == MultitenancyConstants.Root.Key) throw new Exception("Invalid Tenant");
        IsActive = true;
    }

    public void Deactivate()
    {
        if (Key == MultitenancyConstants.Root.Key) throw new Exception("Invalid Tenant");
        IsActive = false;
    }
}