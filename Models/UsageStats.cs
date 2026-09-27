using System.Text.Json;

namespace OddSockets.Models;

/// <summary>
/// Headline usage analytics for the account that owns the configured API key,
/// as returned by <c>GET {managerUrl}/api/tenant/usage</c>.
///
/// HONESTY: each tile is nullable. Any tile the server cannot compute yet comes
/// back as <c>null</c> and is preserved verbatim here — a <c>null</c> tile is
/// never coerced to 0, so callers can render an em-dash instead of a fabricated
/// zero.
/// </summary>
public class UsageStats
{
    /// <summary>
    /// Monthly active users, or <c>null</c> if the server could not compute it.
    /// </summary>
    public long? Mau { get; set; }

    /// <summary>
    /// Daily active users, or <c>null</c> if the server could not compute it.
    /// </summary>
    public long? Dau { get; set; }

    /// <summary>
    /// Total messages, or <c>null</c> if the server could not compute it.
    /// </summary>
    public long? TotalMessages { get; set; }

    /// <summary>
    /// Error rate, or <c>null</c> if the server could not compute it.
    /// </summary>
    public double? ErrorRate { get; set; }

    /// <summary>
    /// The owner scope the tiles are aggregated over.
    /// </summary>
    public string? OwnerScope { get; set; }

    /// <summary>
    /// Optional additional detail returned by the server, or <c>null</c>.
    /// </summary>
    public JsonElement? Detail { get; set; }

    /// <summary>
    /// The server-side timestamp for this snapshot.
    /// </summary>
    public string? Timestamp { get; set; }
}
