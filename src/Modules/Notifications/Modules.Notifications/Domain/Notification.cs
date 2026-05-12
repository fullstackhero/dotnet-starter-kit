using FSH.Framework.Core.Domain;

namespace FSH.Modules.Notifications.Domain;

/// <summary>
/// A single inbox row for the bell-icon UI. One notification per (user, event). Source modules
/// hand a payload to the integration event bus; <c>NotificationsModule</c> writes the row.
/// Body / link are denormalized for display so the inbox doesn't have to follow back into the
/// source module to render.
/// </summary>
public sealed class Notification : AggregateRoot<Guid>
{
    public string UserId { get; private set; } = default!;

    /// <summary>Logical event type, e.g. <c>chat.mention</c>. Used by the UI to pick an icon.</summary>
    public string Type { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Body { get; private set; }
    public string? Link { get; private set; }

    /// <summary>Originating module name (e.g. <c>Chat</c>) — for grouping + filtering.</summary>
    public string Source { get; private set; } = default!;

    /// <summary>Opaque JSON blob — the source module owns the shape.</summary>
    public string MetadataJson { get; private set; } = "{}";

    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { }

    public static Notification Create(
        string userId,
        string type,
        string title,
        string? body,
        string? link,
        string source,
        object? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new Notification
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Type = type,
            Title = title,
            Body = body,
            Link = link,
            Source = source,
            MetadataJson = metadata is null
                ? "{}"
                : System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void MarkRead()
    {
        ReadAtUtc ??= DateTime.UtcNow;
    }
}
