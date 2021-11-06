using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DN.WebApi.Application.Abstractions.Services.General;
using DN.WebApi.Application.Abstractions.Services.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DN.WebApi.Bootstrapper.Controllers.Identity
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditsController : ControllerBase
    {
        private readonly ICurrentUser _user;
        private readonly IAuditService _auditService;

        public AuditsController(IAuditService auditService, ICurrentUser user)
        {
            _auditService = auditService;
            _user = user;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyLogsAsync()
        {
            return Ok(await _auditService.GetUserTrailsAsync(_user.GetUserId()));
        }

    }
}