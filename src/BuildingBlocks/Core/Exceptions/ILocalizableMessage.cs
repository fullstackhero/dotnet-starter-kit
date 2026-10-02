namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// Implemented by exceptions whose response Detail can be localized from a resource key.
/// Lets <c>GlobalExceptionHandler</c> translate the body under the request culture while the
/// exception type stays intact — needed for BCL types the audit severity classifier keys off
/// (e.g. <see cref="UnauthorizedAccessException"/>, <see cref="KeyNotFoundException"/>).
/// The <see cref="Exception.Message"/> stays the English fallback for logs and unresolved keys.
/// </summary>
public interface ILocalizableMessage
{
    /// <summary>Resource key resolved against <see cref="ResourceSource"/>; null keeps the literal message.</summary>
    string? MessageKey { get; }

    /// <summary>Format arguments applied to the localized message ({0}, {1}, …).</summary>
    IReadOnlyList<object> MessageArgs { get; }

    /// <summary>Marker type identifying the resource catalog; null uses the shared (Core) catalog.</summary>
    Type? ResourceSource { get; }
}
