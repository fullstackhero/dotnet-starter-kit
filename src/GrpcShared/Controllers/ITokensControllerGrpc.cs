using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface ITokensControllerGrpc
{
    [OperationContract]
    Task<TokenResponse> GetTokenAsync(TokenRequest request, CallContext context = default);

    [OperationContract]
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CallContext context = default);
}
