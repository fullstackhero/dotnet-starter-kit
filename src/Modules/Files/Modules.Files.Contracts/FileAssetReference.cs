namespace FSH.Modules.Files.Contracts;

/// <summary>Owning-feature handle to a FileAsset. Stored on join tables in Catalog/Tickets/etc.</summary>
public sealed record FileAssetReference(Guid Id, string OwnerType, Guid? OwnerId);
