using System.Text.Json.Serialization;

namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// High-level classification of audit events.
/// </summary>
public enum AuditEventType
{
    None = 0,
    EntityChange = 1,
    Security = 2,
    Activity = 3,
    Exception = 4
}

/// <summary>
/// Severity scale aligned with standard logging levels.
/// </summary>
public enum AuditSeverity
{
    None = 0,
    Trace = 1,
    Debug = 2,
    Information = 3,
    Warning = 4,
    Error = 5,
    Critical = 6
}

/// <summary>
/// Security-related actions to track (login, token, role, etc.).
/// </summary>
public enum SecurityAction
{
    None = 0,
    LoginSucceeded = 1,
    LoginFailed = 2,
    TokenIssued = 3,
    TokenRevoked = 4,
    PasswordChanged = 5,
    RoleAssigned = 6,
    RoleRevoked = 7,
    PermissionDenied = 8,
    PolicyFailed = 9,
    ImpersonationStarted = 10,
    ImpersonationEnded = 11
}

/// <summary>
/// Database operations that can trigger entity-change auditing.
/// </summary>
public enum EntityOperation
{
    None = 0,
    Insert = 1,
    Update = 2,
    Delete = 3,
    SoftDelete = 4,
    Restore = 5
}

/// <summary>
/// Logical category of activity events.
/// </summary>
public enum ActivityKind
{
    None = 0,
    Http = 1,
    BackgroundJob = 2,
    Command = 3,
    Query = 4,
    Integration = 5
}

/// <summary>
/// Area or subsystem where an exception originated.
/// </summary>
public enum ExceptionArea
{
    None = 0,
    Api = 1,
    Worker = 2,
    Ui = 3,
    Infra = 4,
    Unknown = 255
}

/// <summary>
/// Indicates which HTTP bodies are captured in activity events.
/// </summary>
[Flags]
[JsonConverter(typeof(NumericEnumConverter<BodyCapture>))]
public enum BodyCapture
{
    None = 0,
    Request = 1,
    Response = 2,
    Both = Request | Response
}

/// <summary>
/// Compact, bitwise tags that provide additional audit metadata.
/// </summary>
[Flags]
[JsonConverter(typeof(NumericEnumConverter<AuditTag>))]
public enum AuditTag
{
    None = 0,
    PiiMasked = 1 << 0,
    OutOfQuota = 1 << 1,
    Sampled = 1 << 2,
    RetainedLong = 1 << 3,
    HealthCheck = 1 << 4,
    Authentication = 1 << 5,
    Authorization = 1 << 6
}