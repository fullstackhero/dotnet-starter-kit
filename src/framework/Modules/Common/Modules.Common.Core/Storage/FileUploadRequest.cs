namespace FSH.Framework.Core.Storage;
public class FileUploadRequest
{
    public string FileName { get; init; } = default!;
    public string ContentType { get; init; } = default!;
    public IReadOnlyList<byte> Data { get; init; } = Array.Empty<byte>();
}