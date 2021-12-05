using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IUsersControllerGrpc
{
    [OperationContract]
    Task<IResult<List<UserDetailsDto>>> GetAllAsync(CallContext context = default);

    [OperationContract]
    Task<IResult<UserDetailsDto>> GetByIdAsync(string userId, CallContext context = default);

    [OperationContract]
    Task<IResult<UserRolesResponse>> GetRolesAsync(string userId, CallContext context = default);

    [OperationContract]
    Task<IResult<string>> AssignRolesAsync(UserRolesRequest request, CallContext context = default);

    [OperationContract]
    Task<IResult<List<PermissionDto>>> GetPermissionsAsync(string id, CallContext context = default);
}
