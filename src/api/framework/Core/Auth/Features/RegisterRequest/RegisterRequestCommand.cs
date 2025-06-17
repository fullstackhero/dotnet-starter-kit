using FSH.Framework.Core.Auth.Models;
using MediatR;

namespace FSH.Framework.Core.Auth.Features.RegisterRequest;

public record RegisterRequestCommand(
    string Email,
    string Username,
    string PhoneNumber,
    string Tckn,
    string Password,
    string FirstName,
    string LastName,
    int ProfessionId,
    DateTime? BirthDate,
    string RegistrationIp,
    string DeviceInfo
) : IRequest<RegisterRequestResponse>;

public record RegisterRequestResponse(
    bool Success,
    string Message,
    string? PhoneNumber = null
); 