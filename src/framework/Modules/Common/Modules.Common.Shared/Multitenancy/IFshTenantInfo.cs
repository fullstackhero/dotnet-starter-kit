namespace FSH.Framework.Shared.Multitenancy;
public interface IFshTenantInfo
{
    string? ConnectionString { get; set; }
}