using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class UpdateBrandRequest : IMustBeValid
{
    [DataMember(Order = 1)]
    public string? Name { get; set; }

    [DataMember(Order = 2)]
    public string? Description { get; set; }
}