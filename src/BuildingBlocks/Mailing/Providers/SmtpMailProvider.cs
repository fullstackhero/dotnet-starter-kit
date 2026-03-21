using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Framework.Mailing.Providers;

public class SmtpMailProvider(
    IMailComposer<MimeMessage> composer,
    IMailTransport<MimeMessage> transport)
    : IMailProvider
{
    public string Name => "SMTP";

    public async Task SendAsync(MailRequest request, CancellationToken ct)
    {
        var message = composer.Compose(request, ct);
        await transport.SendAsync(message, ct);
    }
}