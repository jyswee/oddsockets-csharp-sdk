using System.Collections.Concurrent;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OddSockets.Models;
using OddSockets.Exceptions;
using SocketIOClient;

namespace OddSockets;

/// <summary>
/// Main OddSockets client for real-time messaging.
/// 
/// This class provides the primary interface for connecting to OddSockets
/// and managing channels. It follows the same API pattern as our other SDKs
/// for consistency across programming languages.
/// </summary>
public class OddSocketsClient : IDisposable
{
    private readonly OddSocketsConfig _config;
    private readonly ILogger<OddSocketsClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, OddSocketsChannel> _channels;
    private readonly ConcurrentDictionary<EventType, List<Func<object?, Task>>> _eventHandlers;
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly Timer? _heartbeatTimer;

    private SocketIO? _socket;
    private string? _workerUrl;
    private string? _workerId;
    private ConnectionState _connectionState;
    private int _reconnectAttempts;
    private bool _disposed;
    private string _clientIdentifier;
    private object? _sessionInfo;

    /// <summary>
    /// Gets the current connection state.
    /// </summary>
    public ConnectionState ConnectionState => _connectionState;

    /// <summary>
    /// Gets the user ID for this client.
    /// </summary>
    public string UserId => _config.UserId ?? "anonymous";

    /// <summary>
    /// Gets the assigned worker information.
    /// </summary>
    public (string? WorkerId, string? WorkerUrl) WorkerInfo => (_workerId, _workerUrl);

    /// <summary>
    /// Gets whether the client is connected.
    /// </summary>
    public bool IsConnected => _connectionState == ConnectionState.Connected && _socket?.Connected == true;

    /// <summary>
    /// Gets the client identifier used for session stickiness.
    /// </summary>
    public string ClientIdentifier => _clientIdentifier;

    /// <summary>
    /// Gets the session information.
    /// </summary>
    public object? SessionInfo => _sessionInfo;

    /// <summary>
    /// Initializes a new instance of the OddSocketsClient class.
    /// </summary>
    /// <param name="config">The configuration for the client.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="httpClient">Optional HTTP client instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public OddSocketsClient(OddSocketsConfig config, ILogger<OddSocketsClient>? logger = null, HttpClient? httpClient = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _logger = logger ?? NullLogger<OddSocketsClient>.Instance;
        _httpClient = httpClient ?? new HttpClient();
        _channels = new ConcurrentDictionary<string, OddSocketsChannel>();
        _eventHandlers = new ConcurrentDictionary<EventType, List<Func<object?, Task>>>();
        _connectionSemaphore = new SemaphoreSlim(1, 1);
        _connectionState = ConnectionState.Disconnected;

        // Generate user ID if not provided
        if (string.IsNullOrWhiteSpace(_config.UserId))
        {
            _config.UserId = $"user_{Guid.NewGuid():N}";
        }

        // Generate client identifier for session stickiness
        _clientIdentifier = GenerateClientIdentifier();

        // Setup heartbeat timer
        if (_config.HeartbeatInterval > 0)
        {
            _heartbeatTimer = new Timer(OnHeartbeatTimer, null, Timeout.Infinite, Timeout.Infinite);
        }

        _logger.LogInformation("OddSockets client initialized for user: {UserId}, clientId: {ClientIdentifier}", UserId, _clientIdentifier);

        // Auto-connect if requested
        if (_config.AutoConnect)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Auto-connect failed");
                    await EmitEventAsync(EventType.Error, ex);
                }
            });
        }
    }

    /// <summary>
    /// Connects to the OddSockets platform.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the connection operation.</returns>
    /// <exception cref="OddSocketsConnectionException">Thrown when connection fails.</exception>
    /// <exception cref="OddSocketsAuthenticationException">Thrown when authentication fails.</exception>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(OddSocketsClient));

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_connectionState == ConnectionState.Connected)
            {
                _logger.LogDebug("Already connected");
                return;
            }

            if (_connectionState == ConnectionState.Connecting)
            {
                _logger.LogDebug("Connection already in progress");
                return;
            }

            _connectionState = ConnectionState.Connecting;
            await EmitEventAsync(EventType.Connected, new { UserId, Timestamp = DateTime.UtcNow });

            _logger.LogInformation("Connecting to OddSockets...");

            try
            {
                // Step 1: Get worker assignment from manager
                await GetWorkerAssignmentAsync(cancellationToken);

                // Step 2: Connect to assigned worker
                await ConnectToWorkerAsync(cancellationToken);

                _connectionState = ConnectionState.Connected;
                _reconnectAttempts = 0;

                // Start heartbeat
                StartHeartbeat();

                _logger.LogInformation("Successfully connected to OddSockets");
                await EmitEventAsync(EventType.Connected, new { UserId, Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _connectionState = ConnectionState.Failed;
                _logger.LogError(ex, "Connection failed");
                await EmitEventAsync(EventType.Error, ex);

                // Schedule reconnection if attempts remain
                if (_reconnectAttempts < _config.ReconnectAttempts)
                {
                    _ = Task.Run(() => ScheduleReconnectAsync(cancellationToken));
                }
                else
                {
                    await EmitEventAsync(EventType.MaxReconnectAttemptsReached, new { Attempts = _reconnectAttempts });
                }

                throw;
            }
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Disconnects from the OddSockets platform.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the disconnection operation.</returns>
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            return;

        await _connectionSemaphore.WaitAsync(cancellationToken);
        try
        {
            if (_connectionState == ConnectionState.Disconnected)
            {
                _logger.LogDebug("Already disconnected");
                return;
            }

            _logger.LogInformation("Disconnecting from OddSockets...");

            // Stop heartbeat
            StopHeartbeat();

            // Unsubscribe from all channels
            var unsubscribeTasks = _channels.Values.Select(channel => channel.UnsubscribeAsync(cancellationToken));
            await Task.WhenAll(unsubscribeTasks);

            // Close socket connection
            if (_socket != null)
            {
                await _socket.DisconnectAsync();
                _socket.Dispose();
                _socket = null;
            }

            _connectionState = ConnectionState.Disconnected;
            _workerUrl = null;
            _workerId = null;

            _logger.LogInformation("Disconnected from OddSockets");
            await EmitEventAsync(EventType.Disconnected, new { UserId, Timestamp = DateTime.UtcNow });
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets or creates a channel.
    /// </summary>
    /// <param name="channelName">The channel name.</param>
    /// <returns>A channel instance.</returns>
    /// <exception cref="ArgumentException">Thrown when channel name is invalid.</exception>
    public OddSocketsChannel Channel(string channelName)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name must be a non-empty string", nameof(channelName));

        return _channels.GetOrAdd(channelName, name => new OddSocketsChannel(name, this, _logger));
    }

    /// <summary>
    /// Publishes multiple messages at once.
    /// </summary>
    /// <param name="messages">The messages to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the bulk publish operation with results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when messages is null.</exception>
    /// <exception cref="OddSocketsConnectionException">Thrown when not connected.</exception>
    public async Task<IList<BulkResult>> PublishBulkAsync(IEnumerable<BulkMessage> messages, CancellationToken cancellationToken = default)
    {
        if (messages == null)
            throw new ArgumentNullException(nameof(messages));

        if (!IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        var results = new List<BulkResult>();
        var messageList = messages.ToList();

        foreach (var bulkMessage in messageList)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bulkMessage.Channel) || bulkMessage.Message == null)
                {
                    results.Add(new BulkResult
                    {
                        Success = false,
                        Error = "Missing channel or message"
                    });
                    continue;
                }

                var channel = Channel(bulkMessage.Channel);
                var result = await channel.PublishAsync(bulkMessage.Message, bulkMessage.Options, cancellationToken);
                results.Add(new BulkResult
                {
                    Success = true,
                    Result = result
                });
            }
            catch (Exception ex)
            {
                results.Add(new BulkResult
                {
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Adds an event handler.
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

        _logger.LogDebug("Added event handler for {EventType}", eventType);
    }

    /// <summary>
    /// Adds a synchronous event handler.
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
    /// Removes event handlers.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="handler">Optional specific handler to remove.</param>
    public void Off(EventType eventType, Func<object?, Task>? handler = null)
    {
        if (handler == null)
        {
            _eventHandlers.TryRemove(eventType, out _);
            _logger.LogDebug("Removed all handlers for {EventType}", eventType);
        }
        else if (_eventHandlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                _eventHandlers.TryRemove(eventType, out _);
            }
            _logger.LogDebug("Removed specific handler for {EventType}", eventType);
        }
    }

    /// <summary>
    /// Gets the socket instance for internal use by channels.
    /// </summary>
    /// <returns>The socket instance.</returns>
    internal SocketIO? GetSocket() => _socket;

    /// <summary>
    /// Emits an event to all registered handlers.
    /// </summary>
    /// <param name="eventType">The event type.</param>
    /// <param name="data">The event data.</param>
    /// <returns>A task representing the event emission.</returns>
    internal async Task EmitEventAsync(EventType eventType, object? data)
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
            _logger.LogError(ex, "Error in event handler");
        }
    }

    /// <summary>
    /// Internal: Generate consistent client identifier for session stickiness
    /// </summary>
    /// <returns>Client identifier string</returns>
    private string GenerateClientIdentifier()
    {
        // Create a consistent identifier based on API key and user ID
        var baseId = _config.UserId ?? "default";
        var apiKeyHash = HashString(_config.ApiKey);
        return $"{apiKeyHash}_{baseId}";
    }

    /// <summary>
    /// Internal: Simple hash function for API key
    /// </summary>
    /// <param name="input">String to hash</param>
    /// <returns>Hash string</returns>
    private string HashString(string input)
    {
        if (string.IsNullOrEmpty(input)) return "0";
        
        uint hash = 0;
        foreach (char c in input)
        {
            hash = ((hash << 5) - hash) + c;
            hash = hash & hash; // Convert to 32-bit integer
        }
        
        return Math.Abs((int)hash).ToString("x");
    }

    private async Task GetWorkerAssignmentAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Discover the optimal manager URL automatically
            var managerUrl = await ManagerDiscovery.Instance.DiscoverManagerUrlAsync(_config.ApiKey);
            
            var requestUri = $"{managerUrl}/api/cluster/select-worker";
            var queryParams = new List<string>();
            
            queryParams.Add($"apiKey={Uri.EscapeDataString(_config.ApiKey)}");
            
            if (!string.IsNullOrWhiteSpace(_config.UserId))
            {
                queryParams.Add($"userId={Uri.EscapeDataString(_config.UserId)}");
            }
            
            queryParams.Add($"clientIdentifier={Uri.EscapeDataString(_clientIdentifier)}");
            
            requestUri += "?" + string.Join("&", queryParams);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Add("User-Agent", "OddSockets-DotNet-SDK/0.1.0-beta.1");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_config.Timeout));

            var response = await _httpClient.SendAsync(request, cts.Token);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cts.Token);
            var assignment = JsonSerializer.Deserialize<WorkerAssignment>(content);

            if (assignment?.Url == null)
            {
                throw new OddSocketsConnectionException("Invalid worker assignment response", ErrorCodes.WorkerAssignmentFailed);
            }

            _workerUrl = assignment.Url;
            _workerId = assignment.WorkerId;
            _sessionInfo = assignment.Session;

            await EmitEventAsync(EventType.WorkerAssigned, new
            {
                WorkerId = _workerId,
                WorkerUrl = _workerUrl,
                Session = assignment.Session,
                ClientIdentifier = _clientIdentifier,
                ManagerUrl = managerUrl // Include discovered manager URL for debugging
            });
        }
        catch (Exception error)
        {
            // If manager is offline, try fallback logic
            if (error.Message.Contains("ECONNREFUSED") || error.Message.Contains("ENOTFOUND"))
            {
                throw new OddSocketsConnectionException("Manager is offline. Cannot assign worker without session stickiness.", ErrorCodes.ConnectionFailed);
            }
            throw;
        }
    }

    private async Task ConnectToWorkerAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_workerUrl))
            throw new OddSocketsConnectionException("No worker URL available", ErrorCodes.WorkerAssignmentFailed);

        var options = new SocketIOOptions
        {
            Auth = new Dictionary<string, string>
            {
                ["apiKey"] = _config.ApiKey,
                ["userId"] = UserId
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            ConnectionTimeout = TimeSpan.FromSeconds(_config.Timeout)
        };

        _socket = new SocketIO(_workerUrl, options);

        var connectTcs = new TaskCompletionSource<bool>();
        var errorTcs = new TaskCompletionSource<Exception>();

        _socket.OnConnected += (sender, e) => connectTcs.TrySetResult(true);
        _socket.OnError += (sender, e) => errorTcs.TrySetException(new OddSocketsConnectionException($"Failed to connect to worker: {e}", ErrorCodes.ConnectionFailed));

        SetupSocketEventHandlers();

        await _socket.ConnectAsync();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(15));

        var completedTask = await Task.WhenAny(connectTcs.Task, errorTcs.Task, Task.Delay(-1, cts.Token));

        if (completedTask == connectTcs.Task)
        {
            return; // Success
        }
        else if (completedTask == errorTcs.Task)
        {
            throw await errorTcs.Task;
        }
        else
        {
            throw new OddSocketsConnectionException("Connection timeout", ErrorCodes.OperationTimeout);
        }
    }

    private void SetupSocketEventHandlers()
    {
        if (_socket == null) return;

        _socket.OnDisconnected += async (sender, e) =>
        {
            _connectionState = ConnectionState.Disconnected;
            await EmitEventAsync(EventType.Disconnected, e);

            // Auto-reconnect unless manually disconnected
            if (e != "io client disconnect")
            {
                _ = Task.Run(() => ScheduleReconnectAsync(CancellationToken.None));
            }
        };

        _socket.OnError += async (sender, e) =>
        {
            await EmitEventAsync(EventType.Error, new Exception(e));
        };

        // Forward channel-related events to appropriate channels
        _socket.On("message", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandleMessageAsync(data);
            }
        });

        _socket.On("subscribed", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandleSubscribedAsync(data);
            }
        });

        _socket.On("unsubscribed", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandleUnsubscribedAsync(data);
            }
        });

        _socket.On("published", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandlePublishedAsync(data);
            }
        });

        _socket.On("presence", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandlePresenceAsync(data);
            }
        });

        _socket.On("presence_change", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandlePresenceChangeAsync(data);
            }
        });

        _socket.On("history", async response =>
        {
            var data = response.GetValue<dynamic>();
            var channelName = data?.channel?.ToString();
            if (!string.IsNullOrEmpty(channelName) && _channels.TryGetValue(channelName, out var channel))
            {
                await channel.HandleHistoryAsync(data);
            }
        });
    }

    private async Task ScheduleReconnectAsync(CancellationToken cancellationToken)
    {
        if (_connectionState == ConnectionState.Connected) return;

        _connectionState = ConnectionState.Reconnecting;
        _reconnectAttempts++;

        var delay = Math.Min(1000 * Math.Pow(2, _reconnectAttempts - 1), 30000);

        await EmitEventAsync(EventType.Reconnected, new
        {
            Attempt = _reconnectAttempts,
            MaxAttempts = _config.ReconnectAttempts,
            Delay = delay
        });

        await Task.Delay(TimeSpan.FromMilliseconds(delay), cancellationToken);

        if (_connectionState == ConnectionState.Reconnecting)
        {
            try
            {
                await ConnectAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed", _reconnectAttempts);
            }
        }
    }

    private void StartHeartbeat()
    {
        _heartbeatTimer?.Change(TimeSpan.FromSeconds(_config.HeartbeatInterval), TimeSpan.FromSeconds(_config.HeartbeatInterval));
    }

    private void StopHeartbeat()
    {
        _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnHeartbeatTimer(object? state)
    {
        if (_socket?.Connected == true)
        {
            _logger.LogDebug("Sending heartbeat");
            // In a real implementation, this would send a ping to the server
        }
    }

    /// <summary>
    /// Disposes the client and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            DisconnectAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during disposal");
        }

        _heartbeatTimer?.Dispose();
        _connectionSemaphore.Dispose();
        _socket?.Dispose();
        _httpClient.Dispose();

        GC.SuppressFinalize(this);
    }

    private class WorkerAssignment
    {
        public string? Url { get; set; }
        public string? WorkerId { get; set; }
        public string? Session { get; set; }
    }
}
