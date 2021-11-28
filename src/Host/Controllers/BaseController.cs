using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Host.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
public class BaseController : ControllerBase
{
}