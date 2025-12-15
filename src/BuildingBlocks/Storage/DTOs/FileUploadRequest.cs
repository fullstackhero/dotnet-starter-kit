namespace FSH.Framework.Storage.DTOs;

public class FileUploadRequest
{
    public string FileName { get; set; } = default!;
    public string ContentType { get; set; } = default!;
    public List<byte> Data { get; set; } = [];
}