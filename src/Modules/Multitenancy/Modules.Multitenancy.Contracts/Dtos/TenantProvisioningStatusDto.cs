namespace FSH.Modules.Multitenancy.Contracts.Dtos;

public sealed record TenantProvisioningStepDto(
    string Step,
    string Status,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    string? Error);

public sealed record TenantProvisioningStatusDto(
    string TenantId,
    string Status,
    string CorrelationId,
    string? CurrentStep,
    string? Error,
    DateTime CreatedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    IReadOnlyCollection<TenantProvisioningStepDto> Steps);
