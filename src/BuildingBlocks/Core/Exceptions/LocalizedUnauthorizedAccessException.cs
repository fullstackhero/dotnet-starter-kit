namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// <see cref="UnauthorizedAccessException"/> whose 401 response Detail is localized via
/// <see cref="MessageKey"/>. Subclasses the BCL type on purpose so the audit severity classifier
/// (which maps <see cref="UnauthorizedAccessException"/> to Warning) keeps working, while the body
/// still translates under the request culture. The base message stays the English log fallback.
/// </summary>
public sealed class LocalizedUnauthorizedAccessException : UnauthorizedAccessException, ILocalizableMessage
{
    public string? MessageKey { get; init; }
    public IReadOnlyList<object> MessageArgs { get; init; } = [];
    public Type? ResourceSource { get; init; }

    public LocalizedUnauthorizedAccessException()
    {
    }

    public LocalizedUnauthorizedAccessException(string message)
        : base(message)
    {
    }

    public LocalizedUnauthorizedAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
