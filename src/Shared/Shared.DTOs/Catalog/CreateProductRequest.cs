using DN.WebApi.Shared.DTOs.Storage;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class CreateProductRequest : IMustBeValid
{
    [DataMember(Order = 1)]
    public string? Name { get; set; }

    [DataMember(Order = 2)]
    public string? Description { get; set; }

    [DataMember(Order = 3)]
    public decimal Rate { get; set; }

    [DataMember(Order = 4)]
    public Guid BrandId { get; set; }

    [DataMember(Order = 5)]
    public FileUploadRequest? Image { get; set; }
}