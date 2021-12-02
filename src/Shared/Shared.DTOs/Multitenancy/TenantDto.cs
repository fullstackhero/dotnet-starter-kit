using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Multitenancy;

[DataContract]
public class TenantDto
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Key { get; set; }

    [DataMember(Order = 4)]
    public string? AdminEmail { get; set; }

    [DataMember(Order = 5)]
    public string? ConnectionString { get; set; }

    [DataMember(Order = 6)]
    public bool IsActive { get; set; }

    [DataMember(Order = 7)]
    public DateTime ValidUpto { get; set; }
}