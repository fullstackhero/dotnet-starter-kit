using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace GrpcShared.Models;

[DataContract]
public class ConfirmPhoneNumberRequestGrpc
{
    [DataMember(Order = 1)]
    [Required]
    public string? UserId { get; set; }

    [DataMember(Order = 2)]
    [Required]
    public string? Code { get; set; }
}
