using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UserRolesRequest
{
    [DataMember(Order = 1)]
    public string Id { get; set; }

    [DataMember(Order = 2)]
    public List<UserRoleDto> UserRoles { get; set; }
}