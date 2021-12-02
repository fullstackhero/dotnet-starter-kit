using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Identity;
using GrpcShared.Models;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IRoleClaimsControllerGrpc
{
    [OperationContract]
    public Task<Result<List<RoleClaimResponse>>> GetAllAsync(CallContext context = default);

    [OperationContract]
    public Task<Result<List<RoleClaimResponse>>> GetAllByRoleIdAsync(string roleId, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> PostAsync(RoleClaimRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> DeleteAsync(DeleteRequestGrpc request, CallContext context = default);
}
