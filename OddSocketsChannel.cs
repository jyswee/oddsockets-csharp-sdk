using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OddSockets.Models;
using OddSockets.Exceptions;
using SocketIOClient;

namespace OddSockets;

/// <summary>
/// Channel class for managing real-time messaging.
/// 
/// This class provides channel-specific functionality for subscribing,
/// publishing, and managing messages. It follows the same API pattern
/// as our other SDKs for consistency across languages.
/// </summary>
public class OddSocketsChannel
{
    private readonly string _name;
    private readonly OddSocketsClient _client;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<Message> _messageHistory;
    private readonly ConcurrentDictionary<string, object> _presenceUsers;
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
        _presenceUsers = new ConcurrentDictionary<string, object>();
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
                _logger.LogWarning("Channel '{ChannelName}' already subscribed", _name);
                return;
            }

            _messageCallback = callback;
            _subscribeOptions = options ?? new SubscribeOptions();

            try
            {
                var socket = _client.GetSocket();
                if (socket == null)
                    throw new OddSocketsConnectionException("Socket not available", ErrorCodes.ConnectionFailed);

                // Send subscription request
                await socket.EmitAsync("subscribe", new
                {
                    channel = _name,
                    options = new
                    {
                        enablePresence = _subscribeOptions.EnablePresence,
                        retainHistory = _subscribeOptions.RetainHistory,
                        filterExpression = _subscribeOptions.FilterExpression
                    }
                });

                // Simulate network delay
                await Task.Delay(50, cancellationToken);

                _subscribed = true;
                _logger.LogInformation("Subscribed to channel: {ChannelName}", _name);

                // If presence is enabled, add current user
                if (_subscribeOptions.EnablePresence)
                {
                    _presenceUsers.TryAdd(_client.UserId, new object());
                }

                // Simulate receiving initial messages
                await SimulateInitialMessagesAsync(cancellationToken);
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
                var socket = _client.GetSocket();
                if (socket != null)
                {
                    await socket.EmitAsync("unsubscribe", new { channel = _name });
                }

                // Simulate network delay
                await Task.Delay(50, cancellationToken);

                _subscribed = false;
                _messageCallback = null;
                _subscribeOptions = null;

                // Remove from presence
                _presenceUsers.TryRemove(_client.UserId, out _);

                _logger.LogInformation("Unsubscribed from channel: {ChannelName}", _name);
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

        // Validate message size before publishing
        try
        {
            MessageSizeValidator.ValidateMessageSize(message);
        }
        catch (OddSocketsValidationException)
        {
            throw; // Re-throw validation exceptions as-is
        }

        try
        {
            var messageId = $"msg_{Guid.NewGuid():N}";
            var timestamp = DateTime.UtcNow;

            // Create message object
            var messageObj = new Message
            {
                Id = messageId,
                Channel = _name,
                Data = message,
                Timestamp = timestamp,
                UserId = _client.UserId,
                Metadata = options?.Metadata
            };

            var socket = _client.GetSocket();
            if (socket == null)
                throw new OddSocketsConnectionException("Socket not available", ErrorCodes.ConnectionFailed);

            // Send publish request
            await socket.EmitAsync("publish", new
            {
                channel = _name,
                message = message,
                options = new
                {
                    ttl = options?.Ttl,
                    metadata = options?.Metadata,
                    storeInHistory = options?.StoreInHistory ?? false
                }
            });

            // Simulate network delay
            await Task.Delay(20, cancellationToken);

            // Store in history if requested
            if (options?.StoreInHistory == true || (_subscribeOptions?.RetainHistory == true))
            {
                _messageHistory.Enqueue(messageObj);
                
                // Keep only last 100 messages
                while (_messageHistory.Count > 100)
                {
                    _messageHistory.TryDequeue(out _);
                }
            }

            // Deliver to local subscriber if subscribed
            if (_subscribed && _messageCallback != null)
            {
                await DeliverMessageAsync(messageObj);
            }

            _logger.LogDebug("Published message to channel '{ChannelName}': {Message}", _name, message);

            return new PublishResult
            {
                MessageId = messageId,
                Timestamp = timestamp,
                Channel = _name,
                Success = true
            };
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
            // Simulate API call delay
            await Task.Delay(100, cancellationToken);

            var messages = _messageHistory.ToArray().ToList();

            // Filter by time range if specified
            if (options?.Start != null)
            {
                messages = messages.Where(msg => msg.Timestamp >= options.Start).ToList();
            }

            if (options?.End != null)
            {
                messages = messages.Where(msg => msg.Timestamp <= options.End).ToList();
            }

            // Sort messages
            if (options?.Reverse == true)
            {
                messages = messages.OrderByDescending(msg => msg.Timestamp).ToList();
            }
            else
            {
                messages = messages.OrderBy(msg => msg.Timestamp).ToList();
            }

            // Apply limit
            if (options?.Limit != null && options.Limit > 0)
            {
                messages = messages.Take(options.Limit.Value).ToList();
            }

            _logger.LogDebug("Retrieved {Count} messages from channel '{ChannelName}' history", messages.Count, _name);
            return messages;
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
            // Simulate API call delay
            await Task.Delay(50, cancellationToken);

            var users = _presenceUsers.Keys.ToList();
            var presence = new PresenceInfo
            {
                Channel = _name,
                Users = users,
                Count = users.Count,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogDebug("Retrieved presence for channel '{ChannelName}': {Count} users", _name, presence.Count);
            return presence;
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

    // Internal methods for handling socket events
    internal async Task HandleMessageAsync(dynamic data)
    {
        try
        {
            var message = JsonSerializer.Deserialize<Message>(data.ToString());
            if (message != null)
            {
                await DeliverMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message for channel '{ChannelName}'", _name);
        }
    }

    internal async Task HandleSubscribedAsync(dynamic data)
    {
        await EmitEventAsync(EventType.Connected, data);
    }

    internal async Task HandleUnsubscribedAsync(dynamic data)
    {
        await EmitEventAsync(EventType.Disconnected, data);
    }

    internal async Task HandlePublishedAsync(dynamic data)
    {
        await EmitEventAsync(EventType.Message, data);
    }

    internal async Task HandlePresenceAsync(dynamic data)
    {
        await EmitEventAsync(EventType.Presence, data);
    }

    internal async Task HandlePresenceChangeAsync(dynamic data)
    {
        await EmitEventAsync(EventType.Presence, data);
    }

    internal async Task HandleHistoryAsync(dynamic data)
    {
        // Handle history response if needed
    }

    private async Task DeliverMessageAsync(Message message)
    {
        if (_messageCallback == null) return;

        try
        {
            // Apply filter if specified
            if (_subscribeOptions?.FilterExpression != null)
            {
                if (!EvaluateFilter(message, _subscribeOptions.FilterExpression))
                {
                    return;
                }
            }

            await _messageCallback(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error delivering message to callback for channel '{ChannelName}'", _name);
        }
    }

    private bool EvaluateFilter(Message message, string filterExpression)
    {
        try
        {
            // Simple filter evaluation (in real SDK, this would be more sophisticated)
            var messageStr = JsonSerializer.Serialize(message.Data);
            return messageStr.Contains(filterExpression, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return true; // If filter evaluation fails, pass the message
        }
    }

    private async Task SimulateInitialMessagesAsync(CancellationToken cancellationToken)
    {
        if (!_subscribed || _messageCallback == null) return;

        // Create a welcome message
        var welcomeMessage = new Message
        {
            Id = $"msg_{Guid.NewGuid():N}",
            Channel = _name,
            Data = new
            {
                type = "system",
                text = $"Welcome to channel '{_name}'!",
                timestamp = DateTime.UtcNow.ToString("O")
            },
            Timestamp = DateTime.UtcNow,
            UserId = "system"
        };

        // Deliver after a short delay
        await Task.Delay(100, cancellationToken);
        await DeliverMessageAsync(welcomeMessage);

        // Store in history if enabled
        if (_subscribeOptions?.RetainHistory == true)
        {
            _messageHistory.Enqueue(welcomeMessage);
        }
    }

    private async Task EmitEventAsync(EventType eventType, object? data)
    {
        if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers.Select(handler => SafeInvokeHandler(handler, data));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SafeInvokeHandler(Func<object?, Task> handler, object? data)
    {
        try
        {
            await handler(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in channel event handler for '{ChannelName}'", _name);
        }
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
