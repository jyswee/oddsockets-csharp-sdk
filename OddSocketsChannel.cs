using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OddSockets.Models;
using OddSockets.Exceptions;

namespace OddSockets;

/// <summary>
/// Channel class for managing real-time messaging.
///
/// This class provides channel-specific functionality for subscribing,
/// publishing, and managing messages. All operations travel over the real
/// Socket.IO connection to the assigned OddSockets worker and are correlated
/// with the worker's response events - there is no local echo or simulation.
/// It follows the same API pattern as our other SDKs for consistency across
/// languages.
/// </summary>
public class OddSocketsChannel
{
    private const int RequestTimeoutMs = 15000;

    private readonly string _name;
    private readonly OddSocketsClient _client;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Message> _messageHistory;
    private readonly SemaphoreSlim _operationSemaphore;

    private bool _subscribed;
    private SubscribeOptions? _subscribeOptions;
    private Func<Message, Task>? _messageCallback;
    private readonly ConcurrentDictionary<EventType, List<Func<object?, Task>>> _eventHandlers;

    /// <summary>
    /// Gets the channel name.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets whether the channel is subscribed.
    /// </summary>
    public bool IsSubscribed => _subscribed;

    /// <summary>
    /// Initializes a new instance of the OddSocketsChannel class.
    /// </summary>
    /// <param name="name">The channel name.</param>
    /// <param name="client">The OddSockets client.</param>
    /// <param name="logger">The logger instance.</param>
    internal OddSocketsChannel(string name, OddSocketsClient client, ILogger logger)
    {
        _name = name;
        _client = client;
        _logger = logger;
        _messageHistory = new ConcurrentQueue<Message>();
        _operationSemaphore = new SemaphoreSlim(1, 1);
        _eventHandlers = new ConcurrentDictionary<EventType, List<Func<object?, Task>>>();

        _logger.LogDebug("Channel '{ChannelName}' initialized", _name);
    }

    /// <summary>
    /// Subscribes to channel messages.
    /// </summary>
    /// <param name="callback">The message callback function.</param>
    /// <param name="options">Optional subscription options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the subscription operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when callback is null.</exception>
    /// <exception cref="OddSocketsConnectionException">Thrown when not connected.</exception>
    /// <exception cref="OddSocketsChannelException">Thrown when subscription fails.</exception>
    public async Task SubscribeAsync(Func<Message, Task> callback, SubscribeOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_subscribed)
            {
                _messageCallback = callback;
                _logger.LogWarning("Channel '{ChannelName}' already subscribed", _name);
                return;
            }

            _subscribeOptions = options ?? new SubscribeOptions();

            try
            {
                var payload = new Dictionary<string, object>
                {
                    ["channel"] = _name,
                    ["options"] = BuildSubscribeOptions(_subscribeOptions)
                };

                // Real request/response: block until the worker acks "subscribed".
                await _client.RequestAsync("subscribe", payload, "subscribed", _name, RequestTimeoutMs);

                _messageCallback = callback;
                _subscribed = true;
                _logger.LogInformation("Subscribed to channel: {ChannelName}", _name);
            }
            catch (OddSocketsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription failed for channel '{ChannelName}'", _name);
                throw new OddSocketsChannelException(
                    $"Failed to subscribe to channel '{_name}'",
                    ErrorCodes.ChannelAccessDenied,
                    _name);
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Subscribes to channel messages with a synchronous callback.
    /// </summary>
    /// <param name="callback">The message callback function.</param>
    /// <param name="options">Optional subscription options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the subscription operation.</returns>
    public async Task SubscribeAsync(Action<Message> callback, SubscribeOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        await SubscribeAsync(message =>
        {
            callback(message);
            return Task.CompletedTask;
        }, options, cancellationToken);
    }

    /// <summary>
    /// Unsubscribes from channel messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the unsubscription operation.</returns>
    public async Task UnsubscribeAsync(CancellationToken cancellationToken = default)
    {
        await _operationSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_subscribed)
            {
                _logger.LogDebug("Channel '{ChannelName}' not subscribed", _name);
                return;
            }

            try
            {
                var payload = new Dictionary<string, object> { ["channel"] = _name };
                await _client.RequestAsync("unsubscribe", payload, "unsubscribed", _name, RequestTimeoutMs);

                _subscribed = false;
                _messageCallback = null;
                _subscribeOptions = null;

                _logger.LogInformation("Unsubscribed from channel: {ChannelName}", _name);
            }
            catch (OddSocketsException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unsubscription failed for channel '{ChannelName}'", _name);
                throw new OddSocketsChannelException(
                    $"Failed to unsubscribe from channel '{_name}'",
                    _name);
            }
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Publishes a message to the channel.
    /// </summary>
    /// <param name="message">The message data to publish.</param>
    /// <param name="options">Optional publish options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the publish operation with the result.</returns>
    /// <exception cref="OddSocketsConnectionException">Thrown when not connected.</exception>
    /// <exception cref="OddSocketsMessageException">Thrown when message publishing fails.</exception>
    /// <exception cref="OddSocketsValidationException">Thrown when message exceeds size limit.</exception>
    public async Task<PublishResult> PublishAsync(object? message, PublishOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        // Validate message size before publishing.
        MessageSizeValidator.ValidateMessageSize(message);

        try
        {
            var payload = new Dictionary<string, object>
            {
                ["channel"] = _name,
                ["message"] = message ?? new { }
            };

            var publishOptions = BuildPublishOptions(options);
            if (publishOptions.Count > 0)
            {
                payload["options"] = publishOptions;
            }

            // Real request/response: the worker returns "published" with the id.
            var resp = await _client.RequestAsync("publish", payload, "published", _name, RequestTimeoutMs);

            var messageId = GetString(resp, "messageId") ?? GetString(resp, "message_id") ?? string.Empty;
            var timestamp = GetTimestamp(resp, "timestamp");

            _logger.LogDebug("Published message to channel '{ChannelName}': {MessageId}", _name, messageId);

            return new PublishResult
            {
                MessageId = messageId,
                Timestamp = timestamp,
                Channel = _name,
                Success = true
            };
        }
        catch (OddSocketsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to channel '{ChannelName}'", _name);
            throw new OddSocketsMessageException(
                $"Failed to publish message to channel '{_name}'",
                ErrorCodes.MessageDeliveryFailed,
                "unknown");
        }
    }

    /// <summary>
    /// Gets message history for the channel.
    /// </summary>
    /// <param name="options">Optional history options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the history retrieval operation with the messages.</returns>
    /// <exception cref="OddSocketsConnectionException">Thrown when not connected.</exception>
    public async Task<IList<Message>> GetHistoryAsync(HistoryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        try
        {
            var payload = new Dictionary<string, object>
            {
                ["channel"] = _name,
                ["count"] = options?.Limit is > 0 ? options.Limit!.Value : 50
            };
            if (options?.Start != null) payload["start"] = options.Start.Value.ToUniversalTime().ToString("O");
            if (options?.End != null) payload["end"] = options.End.Value.ToUniversalTime().ToString("O");

            var resp = await _client.RequestAsync("get_history", payload, "history", _name, RequestTimeoutMs);

            var messages = new List<Message>();
            if (resp.ValueKind == JsonValueKind.Object &&
                resp.TryGetProperty("messages", out var arr) &&
                arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in arr.EnumerateArray())
                {
                    messages.Add(EnvelopeToMessage(el));
                }
            }

            if (options?.Reverse == true)
            {
                messages.Reverse();
            }

            _logger.LogDebug("Retrieved {Count} messages from channel '{ChannelName}' history", messages.Count, _name);
            return messages;
        }
        catch (OddSocketsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get history for channel '{ChannelName}'", _name);
            throw new OddSocketsChannelException(
                $"Failed to get history for channel '{_name}'",
                _name);
        }
    }

    /// <summary>
    /// Gets presence information for the channel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the presence retrieval operation with the presence info.</returns>
    /// <exception cref="OddSocketsConnectionException">Thrown when not connected.</exception>
    public async Task<PresenceInfo> GetPresenceAsync(CancellationToken cancellationToken = default)
    {
        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        try
        {
            var payload = new Dictionary<string, object> { ["channel"] = _name };
            var resp = await _client.RequestAsync("get_presence", payload, "presence", _name, RequestTimeoutMs);

            var users = new List<string>();
            if (resp.ValueKind == JsonValueKind.Object &&
                resp.TryGetProperty("occupants", out var occupants) &&
                occupants.ValueKind == JsonValueKind.Array)
            {
                foreach (var occupant in occupants.EnumerateArray())
                {
                    if (occupant.ValueKind == JsonValueKind.String)
                    {
                        var value = occupant.GetString();
                        if (value != null) users.Add(value);
                    }
                    else if (occupant.ValueKind == JsonValueKind.Object &&
                             occupant.TryGetProperty("userId", out var uid) &&
                             uid.ValueKind == JsonValueKind.String)
                    {
                        var value = uid.GetString();
                        if (value != null) users.Add(value);
                    }
                }
            }

            var count = users.Count;
            if (resp.ValueKind == JsonValueKind.Object &&
                resp.TryGetProperty("occupancy", out var occ) &&
                occ.ValueKind == JsonValueKind.Number &&
                occ.TryGetInt32(out var parsedCount))
            {
                count = parsedCount;
            }

            var presence = new PresenceInfo
            {
                Channel = _name,
                Users = users,
                Count = count,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("Retrieved presence for channel '{ChannelName}': {Count} users", _name, presence.Count);
            return presence;
        }
        catch (OddSocketsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get presence for channel '{ChannelName}'", _name);
            throw new OddSocketsChannelException(
                $"Failed to get presence for channel '{_name}'",
                _name);
        }
    }

    /// <summary>
    /// Adds an event handler for channel events.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="handler">The event handler.</param>
    public void On(EventType eventType, Func<object?, Task> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        _eventHandlers.AddOrUpdate(eventType,
            new List<Func<object?, Task>> { handler },
            (_, existing) =>
            {
                existing.Add(handler);
                return existing;
            });

        _logger.LogDebug("Added event handler for {EventType} on channel '{ChannelName}'", eventType, _name);
    }

    /// <summary>
    /// Adds a synchronous event handler for channel events.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="handler">The event handler.</param>
    public void On(EventType eventType, Action<object?> handler)
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        On(eventType, data =>
        {
            handler(data);
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Removes event handlers for channel events.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    public void Off(EventType eventType, Func<object?, Task>? handler = null)
    {
        if (handler == null)
        {
            _eventHandlers.TryRemove(eventType, out _);
            _logger.LogDebug("Removed all handlers for {EventType} on channel '{ChannelName}'", eventType, _name);
        }
        else if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _eventHandlers.TryRemove(eventType, out _);
            }
            _logger.LogDebug("Removed specific handler for {EventType} on channel '{ChannelName}'", eventType, _name);
        }
    }

    /// <summary>
    /// Internal: handle a real incoming message broadcast (routed by the client).
    /// </summary>
    /// <param name="envelope">The broadcast envelope from the worker.</param>
    internal async Task HandleMessageAsync(JsonElement envelope)
    {
        try
        {
            var message = EnvelopeToMessage(envelope);

            if (_subscribeOptions?.RetainHistory == true)
            {
                _messageHistory.Enqueue(message);
                while (_messageHistory.Count > 100)
                {
                    _messageHistory.TryDequeue(out _);
                }
            }

            var callback = _messageCallback;
            if (callback != null)
            {
                if (_subscribeOptions?.FilterExpression != null &&
                    !EvaluateFilter(message, _subscribeOptions.FilterExpression))
                {
                    return;
                }

                await callback(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message for channel '{ChannelName}'", _name);
        }
    }

    private static Dictionary<string, object> BuildSubscribeOptions(SubscribeOptions options)
    {
        // Worker destructures options with defaults and rejects JSON null, so we
        // only emit concrete values (camelCase to match the wire contract).
        var payload = new Dictionary<string, object>
        {
            ["enablePresence"] = options.EnablePresence,
            ["retainHistory"] = options.RetainHistory
        };
        if (!string.IsNullOrEmpty(options.FilterExpression))
        {
            payload["filterExpression"] = options.FilterExpression!;
        }
        return payload;
    }

    private static Dictionary<string, object> BuildPublishOptions(PublishOptions? options)
    {
        var payload = new Dictionary<string, object>();
        if (options == null) return payload;

        if (options.Ttl.HasValue) payload["ttl"] = options.Ttl.Value;
        if (options.Metadata != null) payload["metadata"] = options.Metadata;
        payload["storeInHistory"] = options.StoreInHistory;
        return payload;
    }

    private Message EnvelopeToMessage(JsonElement envelope)
    {
        string id = GetString(envelope, "id") ?? GetString(envelope, "messageId") ?? string.Empty;

        object? data = null;
        if (envelope.ValueKind == JsonValueKind.Object)
        {
            if (envelope.TryGetProperty("message", out var inner))
            {
                data = inner.Clone();
            }
            else if (envelope.TryGetProperty("data", out var dataEl))
            {
                data = dataEl.Clone();
            }
        }

        string? userId = null;
        if (envelope.ValueKind == JsonValueKind.Object &&
            envelope.TryGetProperty("publisher", out var publisher) &&
            publisher.ValueKind == JsonValueKind.Object &&
            publisher.TryGetProperty("userId", out var uid) &&
            uid.ValueKind == JsonValueKind.String)
        {
            userId = uid.GetString();
        }
        userId ??= GetString(envelope, "userId") ?? GetString(envelope, "user_id");

        return new Message
        {
            Id = id,
            Channel = _name,
            Data = data,
            Timestamp = GetTimestamp(envelope, "timestamp"),
            UserId = userId
        };
    }

    private bool EvaluateFilter(Message message, string filterExpression)
    {
        // A filter that cannot be evaluated is not a match. Swallowing the
        // failure and returning true delivered messages the subscriber had
        // explicitly filtered out, while looking like the filter had run.
        // The exception surfaces to the caller's handler, which logs it.
        var messageStr = JsonSerializer.Serialize(message.Data);

        // IndexOf rather than Contains(string, StringComparison): that overload
        // does not exist on netstandard2.0, which this package also targets.
        return messageStr.IndexOf(filterExpression, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }
        return null;
    }

    private static DateTime GetTimestamp(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(value.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var parsed))
            {
                return parsed.ToUniversalTime();
            }
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var epochMs))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;
            }
        }
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a string representation of the channel.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString()
    {
        return $"Channel(name='{_name}', subscribed={_subscribed}, history={_messageHistory.Count})";
    }
}
