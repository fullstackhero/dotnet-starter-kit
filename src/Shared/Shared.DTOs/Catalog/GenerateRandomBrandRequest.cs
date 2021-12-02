using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Catalog;

[DataContract]
public class GenerateRandomBrandRequest : IMustBeValid
{
    [DataMember(Order = 1)]
    public int NSeed { get; set; }
}