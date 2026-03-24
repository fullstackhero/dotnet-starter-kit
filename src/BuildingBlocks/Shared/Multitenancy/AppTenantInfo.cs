using System.Diagnostics.CodeAnalysis;
using Finbuckle.MultiTenant.Abstractions;

namespace FSH.Framework.Shared.Multitenancy;

public class AppTenantInfo : TenantInfo, IAppTenantInfo
{
    // Parameterless constructor for tooling/EF.
    [SetsRequiredMembers]
    public AppTenantInfo()
    {
        Id = string.Empty;
        Identifier = string.Empty;
    }

    [SetsRequiredMembers]
    public AppTenantInfo(string id, string identifier, string? name = null)
    {
        Id = id;
        Identifier = identifier;
        Name = name;
    }

    [SetsRequiredMembers]
    public AppTenantInfo(string id, string name, string? connectionString, string adminEmail, string? issuer = null)
        : this(id, id, name)
    {
        ConnectionString = connectionString ?? string.Empty;
        AdminEmail = adminEmail;
        IsActive = true;
        Issuer = issuer;

        // Add Default 1 Month Validity for all new tenants. Something like a DEMO period for tenants.
        ValidUpto = TimeProvider.System.GetUtcNow().UtcDateTime.AddMonths(1);
    }

    public string ConnectionString { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = default!;
    public bool IsActive { get; set; }
    public DateTime ValidUpto { get; set; }
    public string? Issuer { get; set; }

    public void AddValidity(int months) =>
        ValidUpto = ValidUpto.AddMonths(months);

    public void SetValidity(in DateTime validTill)
    {
        var normalized = validTill;
        ValidUpto = ValidUpto < normalized
            ? normalized
            : throw new InvalidOperationException("Subscription cannot be backdated.");
    }

    public void Activate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid Tenant");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (Id == MultitenancyConstants.Root.Id)
        {
            throw new InvalidOperationException("Invalid Tenant");
        }

        IsActive = false;
    }

    string? IAppTenantInfo.ConnectionString
    {
        get => ConnectionString;
        set => ConnectionString = value ?? throw new InvalidOperationException("ConnectionString can't be null.");
    }
}
