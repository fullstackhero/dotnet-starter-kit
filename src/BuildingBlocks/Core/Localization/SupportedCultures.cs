using System.Collections.Frozen;

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
    /// Frozen rather than an array: a public static array is writable by any caller, and the
    /// whitelist a validator and a culture provider both trust cannot be a mutable global.
    /// Ordinal on purpose — a wrong-case tag is a client bug, not a variant.
    /// </summary>
    public static readonly FrozenSet<string> Tags =
        new[] { "en-US", "pt-BR" }.ToFrozenSet(StringComparer.Ordinal);
}
