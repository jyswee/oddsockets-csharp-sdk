namespace OddSockets.Models;

/// <summary>
/// Represents the connection state of the OddSockets client.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// The client is disconnected.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The client is connecting.
    /// </summary>
    Connecting,

    /// <summary>
    /// The client is connected.
    /// </summary>
    Connected,

    /// <summary>
    /// The client is reconnecting.
    /// </summary>
    Reconnecting,

    /// <summary>
    /// The connection has failed.
    /// </summary>
    Failed
}

/// <summary>
/// Represents different event types emitted by the OddSockets client.
/// </summary>
public enum EventType
{
    /// <summary>
    /// Emitted when the client connects.
    /// </summary>
    Connected,

    /// <summary>
    /// Emitted when the client disconnects.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Emitted when the client reconnects.
    /// </summary>
    Reconnected,

    /// <summary>
    /// Emitted when an error occurs.
    /// </summary>
    Error,

    /// <summary>
    /// Emitted when a message is received.
    /// </summary>
    Message,

    /// <summary>
    /// Emitted when presence information changes.
    /// </summary>
    Presence,

    /// <summary>
    /// Emitted when a worker is assigned.
    /// </summary>
    WorkerAssigned,

    /// <summary>
    /// Emitted when reconnection attempts are exhausted.
    /// </summary>
    MaxReconnectAttemptsReached
}

/// <summary>
/// Extension methods for enums.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Converts ConnectionState to string.
    /// </summary>
    /// <param name="state">The connection state.</param>
    /// <returns>String representation of the state.</returns>
    public static string ToStringValue(this ConnectionState state)
    {
        return state switch
        {
            ConnectionState.Disconnected => "disconnected",
            ConnectionState.Connecting => "connecting",
            ConnectionState.Connected => "connected",
            ConnectionState.Reconnecting => "reconnecting",
            ConnectionState.Failed => "failed",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Converts EventType to string.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <returns>String representation of the event type.</returns>
    public static string ToStringValue(this EventType eventType)
    {
        return eventType switch
        {
            EventType.Connected => "connected",
            EventType.Disconnected => "disconnected",
            EventType.Reconnected => "reconnected",
            EventType.Error => "error",
            EventType.Message => "message",
            EventType.Presence => "presence",
            EventType.WorkerAssigned => "worker_assigned",
            EventType.MaxReconnectAttemptsReached => "max_reconnect_attempts_reached",
            _ => "unknown"
        };
    }
}
