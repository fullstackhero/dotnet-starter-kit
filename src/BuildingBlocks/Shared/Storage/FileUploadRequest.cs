namespace FSH.Framework.Shared.Storage;

/// <summary>
/// Represents a file upload request with filename, content type, and data.
/// </summary>
public class FileUploadRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public List<byte> Data { get; set; } = [];
}