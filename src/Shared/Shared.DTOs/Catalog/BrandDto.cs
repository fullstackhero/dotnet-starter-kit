using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class BrandDto : IDto
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Description { get; set; }
}