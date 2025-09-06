namespace FSH.Framework.Core.Domain.Interfaces;

public interface ITenantOwned
{
    string? TenantId { get; }
}