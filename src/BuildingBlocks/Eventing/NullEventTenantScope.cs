using FSH.Framework.Eventing.Abstractions;

namespace FSH.Framework.Eventing;

/// <summary>
/// No-op <see cref="IEventTenantScope"/> used when no multitenancy provider is wired.
/// The multitenancy composition replaces this with a Finbuckle-backed implementation.
/// </summary>
public sealed class NullEventTenantScope : IEventTenantScope
{
    private static readonly IDisposable Noop = new NoopScope();

    public IDisposable Begin(string? tenantId) => Noop;

    private sealed class NoopScope : IDisposable
    {
        public void Dispose() { }
    }
}
