namespace FSH.Framework.Storage.DTOs;

public sealed class FileDownloadResponse
{
    public required Stream Stream { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public long? ContentLength { get; init; }
}