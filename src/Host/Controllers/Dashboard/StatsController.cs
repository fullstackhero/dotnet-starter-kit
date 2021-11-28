using DN.WebApi.Application.Dashboard;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers.Dashboard;

public class StatsController : BaseController
{
    private readonly IStatsService _service;

    public StatsController(IStatsService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var stats = await _service.GetDataAsync();
        return Ok(stats);
    }
}