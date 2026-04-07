using System.Collections.ObjectModel;

namespace FSH.Framework.Mailing.Messages;

public class AzureMailMessage : CloudMailMessage
{
    public IDictionary<string, byte[]> Attachments { get; set; }
        = new Dictionary<string, byte[]>();
}