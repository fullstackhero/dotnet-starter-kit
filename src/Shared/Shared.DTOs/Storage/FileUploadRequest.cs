using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Storage;

[DataContract]
public class FileUploadRequest
{
    [DataMember(Order = 1)]
    public string? Name { get; set; }

    [DataMember(Order = 2)]
    public string? Extension { get; set; }

    [DataMember(Order = 3)]
    public string? Data { get; set; }
}