using DN.WebApi.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Dashboard;

public class StatsController : VersionedApiController
{
    [HttpGet]
    public Task<StatsDto> GetAsync()
    {
        return Mediator.Send(new GetStatsRequest());
    }
}