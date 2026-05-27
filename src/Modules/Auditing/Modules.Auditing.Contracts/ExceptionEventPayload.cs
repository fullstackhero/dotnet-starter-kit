namespace FSH.Modules.Auditing.Contracts;

public sealed record ExceptionEventPayload(
    ExceptionArea Area,
    string ExceptionType,
    string Message,
    IReadOnlyList<string> StackTop,                     // capped frames
    IReadOnlyDictionary<string, object?>? Data,
    string? RouteOrLocation
);