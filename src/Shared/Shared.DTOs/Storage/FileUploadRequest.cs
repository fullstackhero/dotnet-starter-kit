namespace DN.WebApi.Shared.DTOs.Storage;

public class FileUploadRequest
{
    public string Name { get; set; }

    public string Extension { get; set; }

    public string Data { get; set; }
}