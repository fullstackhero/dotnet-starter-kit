namespace FSH.Modules.Files;

/// <summary>
/// Configuration for the Files module. Bound from the <c>Files</c> section of appsettings.json.
/// </summary>
public sealed class FilesOptions
{
    /// <summary>Lifetime of a presigned PUT URL minted by <c>POST /files/upload-url</c>.</summary>
    public int UploadUrlTtlMinutes { get; set; } = 15;

    /// <summary>Lifetime of a presigned GET URL minted by <c>GET /files/{id}/url</c>.</summary>
    public int DownloadUrlTtlMinutes { get; set; } = 5;

    /// <summary>How long a <c>PendingUpload</c> row is allowed to linger before the orphan purge job hard-deletes it.</summary>
    public int OrphanRetentionMinutes { get; set; } = 60;

    /// <summary>How long a soft-deleted FileAsset stays in trash before bytes + row are hard-purged.</summary>
    public int SoftDeleteRetentionDays { get; set; } = 30;

    /// <summary>Categories of files the module accepts, with per-category extension whitelists and size caps.</summary>
    public Dictionary<string, FileCategoryOptions> Categories { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class FileCategoryOptions
{
    public List<string> AllowedExtensions { get; set; } = [];
    public long MaxBytes { get; set; }
}
