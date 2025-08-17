using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace FSH.Starter.Api.Services;

public class WhatsAppStubService : IWhatsAppService
{
    private readonly WhatsAppOptions _options;
    public WhatsAppStubService(IOptions<WhatsAppOptions> options) => _options = options.Value;

    public async Task SendMessageAsync(string to, string body)
    {
        await Task.CompletedTask;
        System.Console.WriteLine($"[WhatsAppStub] -> {to}: {body}");
    }
}
