using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Multitenancy;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface ITenantsControllerGrpc
{
    [OperationContract]
    public Task<Result<TenantDto>> GetAsync(string key, CallContext context = default);

    [OperationContract]
    public Task<Result<List<TenantDto>>> GetAllAsync(CallContext context = default);

    [OperationContract]
    public Task<Result<Guid>> CreateAsync(CreateTenantRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> UpgradeSubscriptionAsync(UpgradeSubscriptionRequest request, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> DeactivateTenantAsync(string tenant, CallContext context = default);

    [OperationContract]
    public Task<Result<string>> ActivateTenantAsync(string tenant, CallContext context = default);

    [OperationContract]
    public Result<IEnumerable<string>> GetAllBannedIp(CallContext context = default);

    [OperationContract]
    public Result<bool> UnBanIp(string ipAddress, CallContext context = default);
}