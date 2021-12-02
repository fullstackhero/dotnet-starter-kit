using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class ResetPasswordRequest
{
    [DataMember(Order = 1)]
    [Required]
    public string? Email { get; set; }

    [DataMember(Order = 2)]
    [Required]
    public string? Password { get; set; }

    [DataMember(Order = 3)]
    [Required]
    public string? Token { get; set; }
}