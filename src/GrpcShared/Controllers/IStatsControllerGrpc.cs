using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Dashboard;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IStatsControllerGrpc
{
    [OperationContract]
    public Task<Result<StatsDto>> GetAsync(CallContext context = default);
}
