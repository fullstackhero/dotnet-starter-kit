using FSH.Framework.Mailing.Contracts;

namespace FSH.Framework.Mailing.Factory;

public class MailProviderFactory(IEnumerable<IMailProvider> providers)
{
    private readonly Dictionary<MailProviderType, IMailProvider> _providers =
        providers.ToDictionary(p => p.ProviderType);

    public IMailProvider Get(MailProviderType type)
        => _providers.TryGetValue(type, out var provider)
            ? provider
            : throw new NotSupportedException($"Mail provider {type} not registered.");
}
