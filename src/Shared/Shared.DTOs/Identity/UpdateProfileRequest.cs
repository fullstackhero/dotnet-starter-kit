using DN.WebApi.Shared.DTOs.FileStorage;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UpdateProfileRequest : IMustBeValid
{
    [DataMember(Order = 1)]
    [Required]
    public string? FirstName { get; set; }

    [DataMember(Order = 2)]
    [Required]
    public string? LastName { get; set; }

    [DataMember(Order = 3)]
    public string? PhoneNumber { get; set; }

    [DataMember(Order = 4)]
    [Required]
    public string? Email { get; set; }

    [DataMember(Order = 5)]
    public FileUploadRequest? Image { get; set; }
}