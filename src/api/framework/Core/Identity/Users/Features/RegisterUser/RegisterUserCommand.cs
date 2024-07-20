using System.Text.Json.Serialization;
using MediatR;

namespace FSH.Framework.Core.Identity.Users.Features.RegisterUser;
public class RegisterUserCommand : IRequest<RegisterUserResponse>
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;
    public string? PhoneNumber { get; set; }

    [JsonIgnore]
    public string? Origin { get; set; }
}
