using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace FSH.Starter.Api.Services;

public class TwilioWhatsAppService : IWhatsAppService
{
    private readonly WhatsAppOptions _options;
    public TwilioWhatsAppService(IOptions<WhatsAppOptions> options)
    {
        _options = options.Value;
        TwilioClient.Init(_options.AccountSid, _options.AuthToken);
    }

    public async Task SendMessageAsync(string to, string body)
    {
        var from = new PhoneNumber($"whatsapp:{_options.FromNumber}");
        var dest = new PhoneNumber($"whatsapp:{to}");
        await MessageResource.CreateAsync(from: from, to: dest, body: body);
    }
}
