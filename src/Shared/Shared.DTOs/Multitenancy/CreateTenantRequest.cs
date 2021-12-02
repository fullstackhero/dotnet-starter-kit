using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Multitenancy;

[DataContract]
public class CreateTenantRequest
{
    [DataMember(Order = 1)]
    public string? Name { get; set; }

    [DataMember(Order = 2)]
    public string? Key { get; set; }

    [DataMember(Order = 3)]
    public string? AdminEmail { get; set; }

    [DataMember(Order = 4)]
    public string? ConnectionString { get; set; }
}