using System.Linq;
using System.Net;

namespace FSH.Framework.Core.Exceptions;

/// <summary>
/// FullStackHero exception used for consistent error handling across the stack.
/// Includes HTTP status codes and optional detailed error messages.
/// </summary>
public class CustomException : Exception, ILocalizableMessage
{
    /// <summary>
    /// A list of error messages (e.g., validation errors, business rules).
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; }

    /// <summary>
    /// The HTTP status code associated with this exception.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Optional resource key resolved against <see cref="ResourceSource"/> to localize the
    /// response body under the request culture. When null, the literal <see cref="Exception.Message"/>
    /// is used. The message itself always stays the (English) fallback for logs.
    /// Set through <see cref="Localized{TResource}(string, object[])"/> or <see cref="Localized(string, object[])"/>.
    /// </summary>
    public string? MessageKey { get; private set; }

    /// <summary>
    /// Format arguments applied to the localized message ({0}, {1}, …).
    /// </summary>
    public IReadOnlyList<object> MessageArgs { get; private set; } = [];

    /// <summary>
    /// Marker type identifying the resource catalog for <see cref="MessageKey"/>. When null,
    /// the shared (Core) catalog is used; module-specific keys point it at the module's own catalog.
    /// </summary>
    public Type? ResourceSource { get; private set; }

    /// <summary>
    /// Localizes the response body from the catalog of <typeparamref name="TResource"/> (a module's
    /// marker, e.g. <c>IdentityResources</c>), keeping the English message as the log fallback:
    /// <c>throw new NotFoundException($"User {id} not found.").Localized&lt;IdentityResources&gt;("Identity.UserNotFound", id);</c>
    /// </summary>
    /// <typeparam name="TResource">Marker type of the resource catalog that declares <paramref name="key"/>.</typeparam>
    /// <param name="key">Resource key; must exist in the neutral catalog (enforced by Architecture.Tests).</param>
    /// <param name="args">Format arguments for the localized message's {0}, {1}, … placeholders.</param>
    /// <returns>This exception, so the call chains onto <c>throw new ...</c>.</returns>
    // The setters are private rather than init so this is the single way to attach a key: the
    // analyzers reject an init property with a hand-written backing field (S2292), and two
    // syntaxes for the same thing would let half the codebase drift back to the long form.
    public CustomException Localized<TResource>(string key, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(args);
        MessageKey = key;
        MessageArgs = args;
        ResourceSource = typeof(TResource);
        return this;
    }

    /// <summary>
    /// Localizes the response body from the shared (Core) catalog, for the generic keys every
    /// module uses (e.g. <c>Error.NoCurrentUser</c>).
    /// </summary>
    /// <param name="key">Resource key; must exist in the shared neutral catalog (enforced by Architecture.Tests).</param>
    /// <param name="args">Format arguments for the localized message's {0}, {1}, … placeholders.</param>
    /// <returns>This exception, so the call chains onto <c>throw new ...</c>.</returns>
    public CustomException Localized(string key, params object[] args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(args);
        MessageKey = key;
        MessageArgs = args;
        ResourceSource = null;
        return this;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with default message and internal server error status.
    /// </summary>
    public CustomException()
        : this("An error occurred.", Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public CustomException(string message)
        : this(message, Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with specified message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    public CustomException(string message, Exception innerException)
        : this(message, innerException, Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with message, errors, and status code.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with full parameters.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <param name="errors">Collection of detailed error messages.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        Exception innerException,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message, innerException)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomException"/> class with message, inner exception, and status code.
    /// </summary>
    /// <param name="message">The main error message.</param>
    /// <param name="innerException">The inner exception that caused this exception.</param>
    /// <param name="statusCode">The HTTP status code associated with this exception.</param>
    public CustomException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode)
        : this(message, innerException, Enumerable.Empty<string>(), statusCode)
    {
    }
}