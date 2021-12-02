using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class ProductDetailsDto : IDto
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Description { get; set; }

    [DataMember(Order = 4)]
    public decimal Rate { get; set; }

    [DataMember(Order = 5)]
    public string? ImagePath { get; set; }

    [DataMember(Order = 6)]
    public BrandDto? Brand { get; set; }
}