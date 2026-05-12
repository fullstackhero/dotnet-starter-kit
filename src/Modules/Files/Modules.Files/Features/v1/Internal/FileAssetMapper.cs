using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Domain;

namespace FSH.Modules.Files.Features.v1.Internal;

/// <summary>
/// Shared mapper from FileAsset → FileAssetDto so handlers don't duplicate the projection.
/// </summary>
internal static class FileAssetMapper
{
    public static FileAssetDto ToDto(FileAsset f, string? publicUrl = null) =>
        new(f.Id, f.OwnerType, f.OwnerId, f.OriginalFileName, f.ContentType, f.SizeBytes,
            (int)f.Visibility, (int)f.Status, (int)f.ScanStatus, f.CreatedAtUtc, publicUrl);
}
