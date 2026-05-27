namespace FSH.Framework.Web.Sse;

/// <summary>
/// Represents a Server-Sent Event to push to connected clients.
/// </summary>
/// <param name="EventType">The event type (maps to SSE 'event:' field).</param>
/// <param name="Data">The event data (maps to SSE 'data:' field). Typically JSON.</param>
/// <param name="Id">Optional event ID for client reconnection tracking.</param>
public sealed record SseEvent(string EventType, string Data, string? Id = null);
