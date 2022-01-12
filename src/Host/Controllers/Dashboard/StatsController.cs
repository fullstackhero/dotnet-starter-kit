using DN.WebApi.Application.Dashboard;

namespace DN.WebApi.Host.Controllers.Dashboard;

public class StatsController : VersionedApiController
{
    [HttpGet]
    public Task<StatsDto> GetAsync()
    {
        return Mediator.Send(new GetStatsRequest());
    }
}