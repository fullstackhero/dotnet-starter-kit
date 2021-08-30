using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseController : ControllerBase
    {
    }
}