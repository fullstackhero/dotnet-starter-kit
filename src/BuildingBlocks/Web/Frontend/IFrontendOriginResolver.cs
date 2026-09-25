namespace FSH.Framework.Web.Frontend;

/// <summary>
/// Resolves the front-end (SPA) origin used to build user-facing links inside e-mails and
/// notifications. Framework-level so any module that sends such links (Identity, Notifications,
/// Billing, Tickets, …) resolves the origin the same way.
/// </summary>
public interface IFrontendOriginResolver
{
    /// <summary>
    /// Origin for a link that lands on the SPA the caller is currently using — self-service flows
    /// (password reset, self-registration) where the request comes from the user's own app.
    /// Validates the request <c>Origin</c> header against <see cref="FrontendOptions.AllowedOrigins"/>
    /// and returns the canonical matching entry (never the client's raw casing). Falls back to
    /// <see cref="FrontendOptions.DefaultOrigin"/> when the request carries no <c>Origin</c> header.
    /// Throws a 400-mapped exception when a header is present but not allow-listed — a forged origin
    /// must never reach an e-mail.
    /// </summary>
    string ResolveForCurrentRequest();

    /// <summary>
    /// Origin for a link whose recipient is not the caller — operator-driven flows (an admin
    /// registering or re-inviting a tenant user, whose confirmation link must land on the tenant's
    /// app, not the operator's) — or where no HTTP request exists (background jobs). Returns
    /// <see cref="FrontendOptions.DefaultOrigin"/>.
    /// </summary>
    string ResolveDefault();
}
