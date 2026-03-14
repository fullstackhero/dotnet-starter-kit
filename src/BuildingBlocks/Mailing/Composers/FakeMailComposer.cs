using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Models;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FSH.Framework.Mailing.Composers;
public class FakeMailComposer(IOptions<MailOptions> settings) : IMailComposer
{
    private readonly MailOptions _settings = settings!.Value;

    public async Task<MimeMessage> Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = new MimeMessage();

        email.From.Add(
            new MailboxAddress(
                _settings.DisplayName,
                _settings.From));

        foreach (string address in request.To) 
            email.To.Add(MailboxAddress.Parse(address));

        email.Subject = request.Subject;

        email.Body = new TextPart()
        {
            Text = request.Body
        };

        return email;
    }
}