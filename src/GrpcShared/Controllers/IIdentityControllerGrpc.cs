using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IIdentityControllerGrpc
{
    [OperationContract]
    public Task<IResult<string>> RegisterAsync(RegisterRequest request, CallContext context = default);

    [OperationContract]
    public Task<IResult<string>> ConfirmEmailAsync(ConfirmEmailRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<IResult<string>> ConfirmPhoneNumberAsync(ConfirmPhoneNumberRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<IResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CallContext context = default);

    [OperationContract]
    public Task<IResult<string>> ResetPasswordAsync(ResetPasswordRequest request, CallContext context = default);

    [OperationContract]
    public Task<IResult<string>> UpdateProfileAsync(UpdateProfileRequest request, CallContext context = default);

    [OperationContract]
    public Task<IResult<UserDetailsDto>> GetProfileDetailsAsync(CallContext context = default);
}