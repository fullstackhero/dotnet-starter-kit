using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IRolesControllerGrpc
{
    [OperationContract]
    public Task<Result<List<RoleDto>>> GetListAsync(CallContext context = default);

    [OperationContract]
    public Task<Result<RoleDto>> GetByIdAsync(string id, CallContext context = default);

    [OperationContract]
    public Task<Result<List<PermissionDto>>> GetPermissionsAsync(string id, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> UpdatePermissionsAsync(UpdatePermissionRequestGrpc request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> RegisterRoleAsync(RoleRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> DeleteAsync(string id, CallContext context = default);
}
