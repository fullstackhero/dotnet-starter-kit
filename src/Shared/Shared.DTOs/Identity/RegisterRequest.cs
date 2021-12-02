using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class RegisterRequest
{
    [DataMember(Order = 1)]
    [Required]
    public string? FirstName { get; set; }

    [DataMember(Order = 2)]
    [Required]
    public string? LastName { get; set; }

    [DataMember(Order = 3)]
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [DataMember(Order = 4)]
    [Required]
    [MinLength(6)]
    public string? UserName { get; set; }

    [DataMember(Order = 5)]
    [Required]
    [MinLength(6)]
    public string? Password { get; set; }

    [DataMember(Order = 6)]
    [Required]
    [Compare("Password")]
    public string? ConfirmPassword { get; set; }

    [DataMember(Order = 7)]
    [Required]
    public string? PhoneNumber { get; set; }
}