using FL_CRMS_ERP_WEBAPI.Application.Dashboard;

namespace FL_CRMS_ERP_WEBAPI.Host.Controllers.Dashboard;

public class DashboardController : VersionedApiController
{
    [HttpGet]
    [MustHavePermission(FLAction.View, FLResource.Dashboard)]
    [OpenApiOperation("Get statistics for the dashboard.", "")]
    public Task<StatsDto> GetAsync()
    {
        return Mediator.Send(new GetStatsRequest());
    }
}