namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// <see cref="KeyNotFoundException"/> whose 404 response Detail is localized via <see cref="MessageKey"/>.
/// Subclasses the BCL type on purpose so audit exception-type fixtures and severity classification that
/// key off <see cref="KeyNotFoundException"/> keep working, while the body still translates under the
/// request culture. The base message stays the English log fallback.
/// </summary>
public sealed class LocalizedKeyNotFoundException : KeyNotFoundException, ILocalizableMessage
{
    public string? MessageKey { get; init; }
    public IReadOnlyList<object> MessageArgs { get; init; } = [];
    public Type? ResourceSource { get; init; }

    public LocalizedKeyNotFoundException()
    {
    }

    public LocalizedKeyNotFoundException(string message)
        : base(message)
    {
    }

    public LocalizedKeyNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
