namespace FSH.Modules.Files.Contracts.v1.DTOs;

/// <summary>
/// Upload lifecycle state of a file asset. Serialized as its string name (global
/// JsonStringEnumConverter), so the SPA sees "PendingUpload"/"Available"/"Quarantined".
/// Lives in Contracts because it is part of the published wire contract (FileAssetDto).
/// </summary>
public enum FileAssetStatus
{
    PendingUpload = 0,
    Available = 1,
    Quarantined = 2
}
