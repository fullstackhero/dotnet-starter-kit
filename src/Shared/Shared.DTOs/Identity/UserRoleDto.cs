using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UserRoleDto
{
    [DataMember(Order = 1)]
    public string? RoleId { get; set; }

    [DataMember(Order = 2)]
    public string? RoleName { get; set; }

    [DataMember(Order = 3)]
    public bool Enabled { get; set; }
}