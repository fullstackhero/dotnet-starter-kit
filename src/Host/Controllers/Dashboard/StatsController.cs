using FSH.WebAPI.Application.Dashboard;

namespace FSH.WebAPI.Host.Controllers.Dashboard;

public class StatsController : VersionedApiController
{
    [HttpGet]
    public Task<StatsDto> GetAsync()
    {
        return Mediator.Send(new GetStatsRequest());
    }
}