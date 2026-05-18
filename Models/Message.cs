using System.Text.Json.Serialization;

namespace OddSockets.Models;

/// <summary>
/// Represents a message received from OddSockets.
/// </summary>
public class Message
{
    /// <summary>
    /// Gets or sets the unique message identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// Gets or sets the message timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the sender's user ID.
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets additional message metadata.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Represents presence information for a channel.
/// </summary>
public class PresenceInfo
{
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of user IDs present in the channel.
    /// </summary>
    [JsonPropertyName("users")]
    public List<string> Users { get; set; } = new();

    /// <summary>
    /// Gets or sets the total number of users present.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets when the presence snapshot was taken.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents the result of a publish operation.
/// </summary>
public class PublishResult
{
    /// <summary>
    /// Gets or sets the unique identifier of the published message.
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the message was published.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the channel the message was published to.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the publish was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

/// <summary>
/// Represents a message for bulk publishing.
/// </summary>
public class BulkMessage
{
    /// <summary>
    /// Gets or sets the channel name.
    /// </summary>
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message payload.
    /// </summary>
    [JsonPropertyName("message")]
    public object? Message { get; set; }

    /// <summary>
    /// Gets or sets the publish options for this message.
    /// </summary>
    [JsonIgnore]
    public PublishOptions? Options { get; set; }

    /// <summary>
    /// Initializes a new instance of the BulkMessage class.
    /// </summary>
    public BulkMessage() { }

    /// <summary>
    /// Initializes a new instance of the BulkMessage class.
    /// </summary>
    /// <param name="channel">The channel name.</param>
    /// <param name="message">The message payload.</param>
    /// <param name="options">The publish options.</param>
    public BulkMessage(string channel, object? message, PublishOptions? options = null)
    {
        Channel = channel;
        Message = message;
        Options = options;
    }
}

/// <summary>
/// Represents the result of a bulk publish operation.
/// </summary>
public class BulkResult
{
    /// <summary>
    /// Gets or sets whether the publish was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the publish result if successful.
    /// </summary>
    [JsonPropertyName("result")]
    public PublishResult? Result { get; set; }

    /// <summary>
    /// Gets or sets the error message if unsuccessful.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// Options for channel subscription.
/// </summary>
public class SubscribeOptions
{
    /// <summary>
    /// Gets or sets whether to enable presence tracking for the channel.
    /// </summary>
    public bool EnablePresence { get; set; }

    /// <summary>
    /// Gets or sets whether to retain message history.
    /// </summary>
    public bool RetainHistory { get; set; }

    /// <summary>
    /// Gets or sets a filter expression for messages.
    /// </summary>
    public string? FilterExpression { get; set; }

    /// <summary>
    /// Creates a new SubscribeOptions builder.
    /// </summary>
    /// <returns>A new SubscribeOptionsBuilder instance.</returns>
    public static SubscribeOptionsBuilder Builder() => new();
}

/// <summary>
/// Builder for SubscribeOptions.
/// </summary>
public class SubscribeOptionsBuilder
{
    private readonly SubscribeOptions _options = new();

    /// <summary>
    /// Enables presence tracking.
    /// </summary>
    /// <param name="enable">Whether to enable presence tracking.</param>
    /// <returns>The builder instance.</returns>
    public SubscribeOptionsBuilder WithPresence(bool enable = true)
    {
        _options.EnablePresence = enable;
        return this;
    }

    /// <summary>
    /// Enables history retention.
    /// </summary>
    /// <param name="retain">Whether to retain history.</param>
    /// <returns>The builder instance.</returns>
    public SubscribeOptionsBuilder WithHistory(bool retain = true)
    {
        _options.RetainHistory = retain;
        return this;
    }

    /// <summary>
    /// Sets a filter expression.
    /// </summary>
    /// <param name="expression">The filter expression.</param>
    /// <returns>The builder instance.</returns>
    public SubscribeOptionsBuilder WithFilter(string expression)
    {
        _options.FilterExpression = expression;
        return this;
    }

    /// <summary>
    /// Builds the options.
    /// </summary>
    /// <returns>The configured SubscribeOptions instance.</returns>
    public SubscribeOptions Build() => _options;
}

/// <summary>
/// Options for message publishing.
/// </summary>
public class PublishOptions
{
    /// <summary>
    /// Gets or sets the time to live for the message in seconds.
    /// </summary>
    public int? Ttl { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for the message.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Gets or sets whether the message should be stored in history.
    /// </summary>
    public bool StoreInHistory { get; set; }

    /// <summary>
    /// Creates a new PublishOptions builder.
    /// </summary>
    /// <returns>A new PublishOptionsBuilder instance.</returns>
    public static PublishOptionsBuilder Builder() => new();
}

/// <summary>
/// Builder for PublishOptions.
/// </summary>
public class PublishOptionsBuilder
{
    private readonly PublishOptions _options = new();

    /// <summary>
    /// Sets the time to live.
    /// </summary>
    /// <param name="ttl">The TTL in seconds.</param>
    /// <returns>The builder instance.</returns>
    public PublishOptionsBuilder WithTtl(int ttl)
    {
        _options.Ttl = ttl;
        return this;
    }

    /// <summary>
    /// Sets metadata.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    /// <returns>The builder instance.</returns>
    public PublishOptionsBuilder WithMetadata(Dictionary<string, object> metadata)
    {
        _options.Metadata = metadata;
        return this;
    }

    /// <summary>
    /// Adds a metadata entry.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The builder instance.</returns>
    public PublishOptionsBuilder WithMetadata(string key, object value)
    {
        _options.Metadata ??= new Dictionary<string, object>();
        _options.Metadata[key] = value;
        return this;
    }

    /// <summary>
    /// Sets whether to store in history.
    /// </summary>
    /// <param name="store">Whether to store in history.</param>
    /// <returns>The builder instance.</returns>
    public PublishOptionsBuilder WithHistory(bool store = true)
    {
        _options.StoreInHistory = store;
        return this;
    }

    /// <summary>
    /// Builds the options.
    /// </summary>
    /// <returns>The configured PublishOptions instance.</returns>
    public PublishOptions Build() => _options;
}

/// <summary>
/// Options for retrieving message history.
/// </summary>
public class HistoryOptions
{
    /// <summary>
    /// Gets or sets the maximum number of messages to retrieve.
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    /// Gets or sets the start time for the history query.
    /// </summary>
    public DateTime? Start { get; set; }

    /// <summary>
    /// Gets or sets the end time for the history query.
    /// </summary>
    public DateTime? End { get; set; }

    /// <summary>
    /// Gets or sets whether messages should be returned in reverse chronological order.
    /// </summary>
    public bool Reverse { get; set; }

    /// <summary>
    /// Creates a new HistoryOptions builder.
    /// </summary>
    /// <returns>A new HistoryOptionsBuilder instance.</returns>
    public static HistoryOptionsBuilder Builder() => new();
}

/// <summary>
/// Builder for HistoryOptions.
/// </summary>
public class HistoryOptionsBuilder
{
    private readonly HistoryOptions _options = new();

    /// <summary>
    /// Sets the limit.
    /// </summary>
    /// <param name="limit">The maximum number of messages.</param>
    /// <returns>The builder instance.</returns>
    public HistoryOptionsBuilder WithLimit(int limit)
    {
        _options.Limit = limit;
        return this;
    }

    /// <summary>
    /// Sets the start time.
    /// </summary>
    /// <param name="start">The start time.</param>
    /// <returns>The builder instance.</returns>
    public HistoryOptionsBuilder WithStart(DateTime start)
    {
        _options.Start = start;
        return this;
    }

    /// <summary>
    /// Sets the end time.
    /// </summary>
    /// <param name="end">The end time.</param>
    /// <returns>The builder instance.</returns>
    public HistoryOptionsBuilder WithEnd(DateTime end)
    {
        _options.End = end;
        return this;
    }

    /// <summary>
    /// Sets whether to reverse the order.
    /// </summary>
    /// <param name="reverse">Whether to reverse the order.</param>
    /// <returns>The builder instance.</returns>
    public HistoryOptionsBuilder WithReverse(bool reverse = true)
    {
        _options.Reverse = reverse;
        return this;
    }

    /// <summary>
    /// Builds the options.
    /// </summary>
    /// <returns>The configured HistoryOptions instance.</returns>
    public HistoryOptions Build() => _options;
}
