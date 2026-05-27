namespace FSH.Modules.Auditing.Contracts;

public sealed record EntityChangeEventPayload(
    string DbContext,
    string? Schema,
    string Table,
    string EntityName,
    string Key,                          // unified string key (e.g., "Id:42" or "TenantId:1|UserId:42")
    EntityOperation Operation,
    IReadOnlyList<PropertyChange> Changes,
    string? TransactionId
);