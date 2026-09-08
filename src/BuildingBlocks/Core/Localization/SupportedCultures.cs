namespace FSH.Framework.Core.Localization;

/// <summary>Canonical set of cultures the platform supports for user-facing localization.</summary>
public static class SupportedCultures
{
    /// <summary>Guaranteed ultimate fallback culture, served by the neutral (un-suffixed) catalog.</summary>
    public const string Default = "en-US";

    /// <summary>
    /// Specific tags a user may persist, the switcher offers, and Accept-Language is matched against.
    /// Deliberately specific-only, with no neutral entries: every catalog is named for a specific
    /// culture (<c>*.pt-BR.resx</c>), matching the front-end catalogs. A request asking for a bare
    /// <c>pt</c>, or for an unsupported variant like <c>pt-PT</c>, therefore resolves to
    /// <see cref="Default"/> rather than being silently served Brazilian strings. Adding a language
    /// means adding its specific tag here plus a <c>*.{tag}.resx</c> per catalog.
    /// </summary>
    public static readonly string[] Tags = ["en-US", "pt-BR"];
}
