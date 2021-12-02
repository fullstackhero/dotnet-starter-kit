using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Identity;

[DataContract]
public class UserDetailsDto
{
    [DataMember(Order = 1)]
    public Guid Id { get; set; }

    [DataMember(Order = 2)]
    public string? UserName { get; set; }

    [DataMember(Order = 3)]
    public string? FirstName { get; set; }

    [DataMember(Order = 4)]
    public string? LastName { get; set; }

    [DataMember(Order = 5)]
    public string? Email { get; set; }

    [DataMember(Order = 6)]
    public bool IsActive { get; set; } = true;

    [DataMember(Order = 7)]
    public bool EmailConfirmed { get; set; }

    [DataMember(Order = 8)]
    public string? PhoneNumber { get; set; }

    [DataMember(Order = 9)]
    public string? ImageUrl { get; set; }
}