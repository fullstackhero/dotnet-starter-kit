using FSH.Framework.Mailing.Contracts;

namespace FSH.Framework.Mailing.Factory;

public class MailProviderFactory(IEnumerable<IMailProvider> providers)
{
    private readonly Dictionary<string, IMailProvider> _providers =
        providers.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

    public IMailProvider Get(string provider)
    {
        if (!_providers.TryGetValue(provider, out var mailProvider))
            throw new NotSupportedException($"Mail provider {provider} not supported");

        return mailProvider;
    }
}
