namespace FSH.Modules.Files.Contracts.v1.DTOs;

/// <summary>
/// File visibility. Public = visible to anyone in the tenant; Private = uploader-only.
/// Serialized as its string name (global JsonStringEnumConverter), so the SPA sends and
/// receives "Public"/"Private" rather than 0/1. Lives in Contracts (not Domain) because it
/// is part of the published wire contract for both commands and DTOs.
/// </summary>
public enum Visibility
{
    Public = 0,
    Private = 1
}
