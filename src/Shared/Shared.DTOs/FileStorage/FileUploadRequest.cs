namespace DN.WebApi.Shared.DTOs.FileStorage;

public class FileUploadRequest
{
    public string? Name { get; set; }

    public string? Extension { get; set; }

    public string? Data { get; set; }
}