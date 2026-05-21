namespace FSH.Modules.Identity.Contracts.DTOs;

/// <summary>
/// One entry in the host-wide permission catalog returned to the SPA so the role editor can
/// render every permission that exists, not just the ones the local TypeScript file remembered.
/// Mirrors <c>FSH.Framework.Shared.Constants.FshPermission</c>; the API surface is the
/// authoritative source — modules contribute via <c>PermissionConstants.Register</c> on
/// startup and the editor reads back through this DTO.
/// </summary>
public sealed record PermissionCatalogEntryDto(
    string Name,
    string Description,
    string Resource,
    string Action,
    bool IsBasic,
    bool IsRoot);
