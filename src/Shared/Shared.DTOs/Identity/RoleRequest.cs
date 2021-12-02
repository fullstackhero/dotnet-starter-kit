using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class RoleRequest
{
    [DataMember(Order = 1)]
    public string? Id { get; set; }

    [DataMember(Order = 2)]
    public string? Name { get; set; }

    [DataMember(Order = 3)]
    public string? Description { get; set; }
}