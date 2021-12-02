using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class ConfirmEmailRequestGrpc
{
    [DataMember(Order = 1)]
    [Required]
    public string? UserId { get; set; }

    [DataMember(Order = 2)]
    [Required]
    public string? Code { get; set; }

    [DataMember(Order = 3)]
    [Required]
    public string? Tenant { get; set; }
}
