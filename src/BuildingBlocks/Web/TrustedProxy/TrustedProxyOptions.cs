namespace FSH.Framework.Web.TrustedProxy;

/// <summary>
/// Trusted reverse-proxy configuration for X-Forwarded-* processing. Behind an ingress
/// (e.g. cloudflared → Caddy → app) the real client IP and scheme arrive via forwarded headers;
/// these settings bound which upstream sources are trusted so a client reaching the app from
/// outside the proxy network cannot forge its own IP/scheme. When no proxies or networks are
/// configured, the framework default (loopback only) stands and forwarded headers from any other
/// source are ignored.
/// <para>
/// Only X-Forwarded-For and X-Forwarded-Proto are honoured. X-Forwarded-Host is deliberately left
/// out: rewriting Request.Host from a header is a host-header injection primitive, and the endpoints
/// that build a public URL from the request (user registration and confirmation e-mails) would then
/// send links pointing wherever the header said. The trade-off is that Request.Host keeps the
/// internal host behind a proxy, and those links carry it.
/// </para>
/// </summary>
public sealed class TrustedProxyOptions
{
    /// <summary>Individual upstream proxy IP addresses whose X-Forwarded-* headers are trusted.</summary>
    public string[] KnownProxies { get; init; } = [];

    /// <summary>Trusted upstream networks in CIDR notation (e.g. "10.0.0.0/8", "172.16.0.0/12").</summary>
    public string[] KnownNetworks { get; init; } = [];

    /// <summary>
    /// Number of proxy hops to unwind from X-Forwarded-For. Must match the real ingress hop count
    /// (cloudflared → Caddy → app is 2). The framework default of 1 reads only the rightmost hop,
    /// which yields the nearest proxy's IP (or an attacker-injected value) in a multi-hop topology.
    /// Must be at least 1: anything lower is rejected at startup, since 0 would silently stop
    /// forwarded-header processing and a negative value would fail every request.
    /// </summary>
    public int ForwardLimit { get; init; } = 1;
}
