namespace FSH.Modules.Files.Contracts.v1.DTOs;

public sealed record FileAssetDto(
    Guid Id,
    string OwnerType,
    Guid? OwnerId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    int Visibility,
    int Status,
    int ScanStatus,
    DateTime CreatedAtUtc,
    string? PublicUrl,
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);
