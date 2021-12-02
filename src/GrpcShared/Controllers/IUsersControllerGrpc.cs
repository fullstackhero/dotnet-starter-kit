using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IUsersControllerGrpc
{
    [OperationContract]
    Task<Result<List<UserDetailsDto>>> GetAllAsync(CallContext context = default);

    [OperationContract]
    Task<Result<UserDetailsDto>> GetByIdAsync(string userId, CallContext context = default);

    [OperationContract]
    Task<Result<UserRolesResponse>> GetRolesAsync(string userId, CallContext context = default);

    [OperationContract]
    Task<Result<string>> AssignRolesAsync(UserRolesRequest request, CallContext context = default);

    [OperationContract]
    Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id, CallContext context = default);
}
