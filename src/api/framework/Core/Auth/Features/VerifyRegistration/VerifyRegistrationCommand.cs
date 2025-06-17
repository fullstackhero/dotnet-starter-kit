using MediatR;

namespace FSH.Framework.Core.Auth.Features.VerifyRegistration;

public record VerifyRegistrationCommand(
    string PhoneNumber,
    string OtpCode
) : IRequest<VerifyRegistrationResponse>;

public record VerifyRegistrationResponse(
    bool Success,
    string Message,
    Guid? UserId = null
); 