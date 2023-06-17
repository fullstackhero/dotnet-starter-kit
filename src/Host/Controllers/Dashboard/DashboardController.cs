using FL_CRMS_ERP_WEBAPI.Application.Dashboard;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Dashboard;

public class DashboardController : VersionedApiController
{
    [HttpGet]
    [MustHavePermission(FSHAction.View, FSHResource.Dashboard)]
    [OpenApiOperation("Get statistics for the dashboard.", "")]
    public Task<StatsDto> GetAsync()
    {
        return Mediator.Send(new GetStatsRequest());
    }
}