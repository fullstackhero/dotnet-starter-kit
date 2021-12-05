using DN.WebApi.Application.Dashboard;
using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs.Dashboard;
using GrpcShared.Controllers;
using ProtoBuf.Grpc;

namespace DN.WebApi.Host.Controllers.Dashboard;

public class StatsControllerGrpc : IStatsControllerGrpc
{
    private readonly IStatsService _service;

    public StatsControllerGrpc(IStatsService service)
    {
        _service = service;
    }

    public async Task<IResult<StatsDto>> GetAsync(CallContext context)
    {
        var stats = await _service.GetDataAsync();
        return stats;
    }
}
