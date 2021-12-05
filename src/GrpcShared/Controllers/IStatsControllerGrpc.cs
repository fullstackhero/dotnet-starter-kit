using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Dashboard;
using ProtoBuf.Grpc;
using System.ServiceModel;

namespace GrpcShared.Controllers;

[ServiceContract]
public interface IStatsControllerGrpc
{
    [OperationContract]
    public Task<IResult<StatsDto>> GetAsync(CallContext context = default);
}
