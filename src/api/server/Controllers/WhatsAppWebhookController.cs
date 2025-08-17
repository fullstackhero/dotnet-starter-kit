using System.Threading.Tasks;
using FSH.Starter.Api.Dtos;
using FSH.Starter.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FSH.Starter.Api.Controllers;

[ApiController]
[Route("api/webhook/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IChatService _chat;
    private readonly IWhatsAppService _wa;

    public WhatsAppWebhookController(IChatService chat, IWhatsAppService wa)
    {
        _chat = chat; _wa = wa;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] WhatsAppMessageDto message)
    {
        var answer = await _chat.ProcessMessageAsync(message);
        await _wa.SendMessageAsync(message.From, answer);
        return Ok(new { reply = answer });
    }
}
