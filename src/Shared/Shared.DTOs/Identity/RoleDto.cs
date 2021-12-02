using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class RoleDto
{
    [DataMember(Order = 1)]
    public string? Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Description { get; set; }

    [DataMember(Order = 4)]
    public string? Tenant { get; set; }
}