namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed class TenantStatusDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTime ValidUpto { get; init; }
    public bool HasConnectionString { get; init; }
    public string AdminEmail { get; init; } = default!;
    public string? Issuer { get; init; }
}