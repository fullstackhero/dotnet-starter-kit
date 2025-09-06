using FSH.Modules.Common.Core.Messaging.CQRS;
using System.Text.Json.Serialization;

namespace FSH.Framework.Identity.Endpoints.v1.Users.RegisterUser;
public class RegisterUserCommand : ICommand<RegisterUserResponse>
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