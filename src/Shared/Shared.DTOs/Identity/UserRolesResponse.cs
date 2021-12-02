using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UserRolesResponse
{
    [DataMember(Order = 1)]
    public List<UserRoleDto> UserRoles { get; set; } = new();
}