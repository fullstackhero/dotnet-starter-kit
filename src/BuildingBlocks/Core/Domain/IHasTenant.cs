namespace FSH.Framework.Core.Domain;

/// <summary>
/// Associates an entity with a tenant.
/// </summary>
public interface IHasTenant
{
    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    string TenantId { get; }
}
