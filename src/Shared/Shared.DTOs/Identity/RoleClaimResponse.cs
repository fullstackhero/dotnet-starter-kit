using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class RoleClaimResponse
{
    [DataMember(Order = 1)]
    public int Id { get; set; }

    [DataMember(Order = 2)]
    public string? RoleId { get; set; }

    [DataMember(Order = 3)]
    public string? Type { get; set; }

    [DataMember(Order = 4)]
    public string? Value { get; set; }

    [DataMember(Order = 5)]
    public string? Description { get; set; }

    [DataMember(Order = 6)]
    public string? Group { get; set; }

    [DataMember(Order = 7)]
    public bool Selected { get; set; }
}