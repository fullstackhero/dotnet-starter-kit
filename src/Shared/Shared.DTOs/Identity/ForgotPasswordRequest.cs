using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class ForgotPasswordRequest
{
    [DataMember(Order = 1)]
    [Required]
    [EmailAddress]
    public string? Email { get; set; }
}