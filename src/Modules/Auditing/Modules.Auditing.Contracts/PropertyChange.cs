namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// A single property delta for entity-change auditing.
/// </summary>
public sealed record PropertyChange(
    string Name,
    string? DataType,   // e.g., "string", "int", "datetime"
    object? OldValue,
    object? NewValue,
    bool IsSensitive    // true => value already masked/hashed
);