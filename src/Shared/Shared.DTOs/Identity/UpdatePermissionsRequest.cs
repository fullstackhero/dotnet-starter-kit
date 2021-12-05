using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UpdatePermissionsRequest
{
    [DataMember(Order = 1)]
    public string? Permission { get; set; }
    [DataMember(Order = 2)]
    public bool Enabled { get; set; }
}