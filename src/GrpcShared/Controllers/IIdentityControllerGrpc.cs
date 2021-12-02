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
    public Task<Result<string>> RegisterAsync(RegisterRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> ConfirmEmailAsync(ConfirmEmailRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> ConfirmPhoneNumberAsync(ConfirmPhoneNumberRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> ResetPasswordAsync(ResetPasswordRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> UpdateProfileAsync(UpdateProfileRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<UserDetailsDto>> GetProfileDetailsAsync(CallContext context = default);
}