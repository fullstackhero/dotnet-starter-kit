using DN.WebApi.Shared.DTOs.Identity;
using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class UpdatePermissionRequestGrpc
{
    [DataMember(Order = 1)]
    public string Id { get; set; }

    [DataMember(Order = 2)]
    public List<UpdatePermissionsRequest> Items { get; set; }
}
