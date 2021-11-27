using DN.WebApi.Shared.DTOs.Storage;

namespace DN.WebApi.Shared.DTOs.Identity;

public class UpdateProfileRequest : IMustBeValid
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public FileUploadRequest Image { get; set; }
}