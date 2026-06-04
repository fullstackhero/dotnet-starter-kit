using FSH.Modules.Files.Contracts.v1.DTOs;
using Mediator;

namespace FSH.Modules.Files.Contracts.v1.Commands;

/// <summary>
/// Flip a file's visibility (Public ↔ Private) after upload. Caller must satisfy
/// <see cref="IFileAccessPolicy.CanChangeVisibilityAsync"/> — by default the uploader.
/// Returns the refreshed FileAssetDto so the SPA can update its preview/list in place.
/// </summary>
public sealed record ChangeFileVisibilityCommand(Guid FileAssetId, Visibility Visibility)
    : ICommand<FileAssetDto>;
