using FSH.Framework.Mailing.Contracts;
using FSH.Framework.Mailing.Messages;
using FSH.Framework.Mailing.Options;
using Microsoft.Extensions.Options;

namespace FSH.Framework.Mailing.Composers;

public class FluentMailComposer(IOptions<MailOptions> settings) : IMailComposer<FluentMailMessage>
{
    public FluentMailMessage Compose(MailRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new()
        {
            Request = request
        };
    }
}
