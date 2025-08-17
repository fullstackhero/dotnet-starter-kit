using System;
using System.Linq;
using System.Threading.Tasks;
using FSH.Starter.Api.Data;
using FSH.Starter.Api.Dtos;
using FSH.Starter.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FSH.Starter.Api.Controllers;

[ApiController]
[Route("api/webhook/twilio")]
public class TwilioWebhookController : ControllerBase
{
    private readonly IChatService _chat;
    private readonly IWhatsAppService _wa;
    private readonly AppDbContext _db;

    public TwilioWebhookController(IChatService chat, IWhatsAppService wa, AppDbContext db)
    {
        _chat = chat; _wa = wa; _db = db;
    }

    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> Receive([FromForm] string From, [FromForm] string To, [FromForm] string Body)
    {
        string Normalize(string s) => s?.Replace("whatsapp:", "", StringComparison.OrdinalIgnoreCase) ?? "";
        var toNumber = Normalize(To);
        var fromNumber = Normalize(From);

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.WhatsAppNumber == toNumber)
                  ?? await _db.Tenants.OrderBy(t => t.CreatedAt).FirstOrDefaultAsync();

        if (tenant is null) return BadRequest("No tenant configured.");

        var message = new WhatsAppMessageDto { TenantId = tenant.Id, From = fromNumber, Body = Body };
        var answer = await _chat.ProcessMessageAsync(message);
        await _wa.SendMessageAsync(fromNumber, answer);
        return Ok();
    }
}
