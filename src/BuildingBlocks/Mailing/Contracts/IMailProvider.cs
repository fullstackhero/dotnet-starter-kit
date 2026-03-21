using FSH.Framework.Mailing.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace FSH.Framework.Mailing.Contracts;

public interface IMailProvider
{
    string Name { get; }

    Task SendAsync(MailRequest request, CancellationToken ct);
}