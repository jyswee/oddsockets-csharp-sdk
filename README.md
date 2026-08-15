# OddSockets .NET SDK

[![NuGet Version](https://img.shields.io/nuget/v/OddSockets.DotNet.SDK)](https://www.nuget.org/packages/OddSockets.DotNet.SDK)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%20Standard%202.0-blue)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Official .NET SDK for [OddSockets](https://oddsockets.com) - Enterprise-ready real-time messaging with automatic load balancing and failover.

## Features

-  **Real-time messaging** with automatic load balancing
- ⚡ **Task-based async/await** patterns for modern .NET
- 🔒 **Strong typing** with nullable reference types
- 🏗️ **Dependency injection** support for ASP.NET Core
- 📦 **Bulk message publishing** for high-throughput scenarios
- 📚 **Message history** and presence tracking
- 🔄 **Automatic reconnection** with exponential backoff
- 🧵 **Thread-safe** operations
- 📝 **Comprehensive logging** support
- 🎯 **Multi-targeting** (.NET 6.0, .NET 8.0, .NET Standard 2.0)

## Installation

### Package Manager Console
```powershell
Install-Package OddSockets.DotNet.SDK
```

### .NET CLI
```bash
dotnet add package OddSockets.DotNet.SDK
```

### PackageReference
```xml
<PackageReference Include="OddSockets.DotNet.SDK" Version="0.1.0-beta.1" />
```

## Quick Start

### Basic Usage

```csharp
using OddSockets;
using OddSockets.Models;

// Create configuration
var config = new OddSocketsConfigBuilder()
    .WithApiKey("ak_live_your_api_key_here")
    .WithUserId("user123")
    .Build();

// Create client
using var client = new OddSocketsClient(config);

// Connect
await client.ConnectAsync();

// Get a channel
var channel = client.Channel("my-channel");

// Subscribe to messages
await channel.SubscribeAsync(message =>
{
    Console.WriteLine($"Received: {message.Data}");
});

// Publish a message
await channel.PublishAsync("Hello, OddSockets!");
```

### ASP.NET Core Integration

```csharp
// Program.cs or Startup.cs
builder.Services.AddSingleton<OddSocketsConfig>(provider =>
    new OddSocketsConfigBuilder()
        .WithApiKey(builder.Configuration["OddSockets:ApiKey"])
        .WithUserId("web-app-user")
        .Build());

builder.Services.AddSingleton<OddSocketsClient>();

// In your controller or service
public class ChatController : ControllerBase
{
    private readonly OddSocketsClient _oddSockets;

    public ChatController(OddSocketsClient oddSockets)
    {
        _oddSockets = oddSockets;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
    {
        var channel = _oddSockets.Channel("chat-room");
        var result = await channel.PublishAsync(message);
        return Ok(result);
    }
}
```

## Configuration

### Using Builder Pattern

```csharp
var config = new OddSocketsConfigBuilder()
    .WithApiKey("ak_live_your_api_key_here")
    .WithManagerUrl("https://connect.oddsockets.tyga.network") // Optional
    .WithUserId("user123") // Optional, auto-generated if not provided
    .WithAutoConnect(true) // Optional, default: true
    .WithReconnectAttempts(5) // Optional, default: 5
    .WithHeartbeatInterval(30) // Optional, default: 30 seconds
    .WithTimeout(10) // Optional, default: 10 seconds
    .Build();
```

### Manager URL

The manager URL is resolved in this order:

1. `WithManagerUrl(...)` (or `OddSocketsConfig.ManagerUrl`)
2. the `ODDSOCKETS_MANAGER_URL` environment variable
3. `https://connect.oddsockets.tyga.network`

It must be an absolute `http://` or `https://` URL, otherwise `Build()` throws
`ArgumentException` with the message `Invalid managerUrl: <value>`. Point it at a
self-hosted or staging manager and the SDK will use that endpoint and nothing else: if it
is unreachable the connection fails with the underlying error rather than falling back to
the public endpoint.

### Using Object Initializer

```csharp
var config = new OddSocketsConfig
{
    ApiKey = "ak_live_your_api_key_here",
    UserId = "user123",
    AutoConnect = true,
    ReconnectAttempts = 3
};
```

## Core Concepts

### Client Management

```csharp
using var client = new OddSocketsClient(config);

// Connection events
client.On(EventType.Connected, data => 
    Console.WriteLine("Connected!"));

client.On(EventType.Disconnected, data => 
    Console.WriteLine("Disconnected"));

client.On(EventType.Error, error => 
    Console.WriteLine($"Error: {error}"));

// Manual connection control
await client.ConnectAsync();
await client.DisconnectAsync();

// Check connection status
if (client.IsConnected)
{
    Console.WriteLine("Ready to send messages!");
}
```

### Channel Operations

```csharp
var channel = client.Channel("my-channel");

// Subscribe with options
await channel.SubscribeAsync(
    message => Console.WriteLine($"Message: {message.Data}"),
    SubscribeOptions.Builder()
        .WithPresence(true)
        .WithHistory(true)
        .WithFilter("important")
        .Build()
);

// Publish with options
var result = await channel.PublishAsync(
    "Hello World!",
    PublishOptions.Builder()
        .WithTtl(3600) // 1 hour
        .WithMetadata("priority", "high")
        .WithHistory(true)
        .Build()
);

Console.WriteLine($"Published: {result.MessageId}");
```

### Bulk Publishing

```csharp
var messages = new List<BulkMessage>
{
    new("channel1", "Message 1"),
    new("channel2", "Message 2"),
    new("channel3", new { type = "notification", text = "Message 3" })
};

var results = await client.PublishBulkAsync(messages);

foreach (var result in results)
{
    if (result.Success)
        Console.WriteLine($"✅ {result.Result?.MessageId}");
    else
        Console.WriteLine($"❌ {result.Error}");
}
```

### Message History

```csharp
var history = await channel.GetHistoryAsync(
    HistoryOptions.Builder()
        .WithLimit(50)
        .WithStart(DateTime.UtcNow.AddHours(-1))
        .WithReverse(true)
        .Build()
);

foreach (var message in history)
{
    Console.WriteLine($"{message.Timestamp}: {message.Data}");
}
```

### Presence Tracking

```csharp
// Enable presence when subscribing
await channel.SubscribeAsync(
    message => { /* handle message */ },
    SubscribeOptions.Builder().WithPresence(true).Build()
);

// Get current presence
var presence = await channel.GetPresenceAsync();
Console.WriteLine($"{presence.Count} users online:");
foreach (var user in presence.Users)
{
    Console.WriteLine($"- {user}");
}

// Listen for presence changes
channel.On(EventType.Presence, data =>
{
    Console.WriteLine($"Presence update: {data}");
});
```

## Advanced Features

### Error Handling

```csharp
try
{
    await channel.PublishAsync("test message");
}
catch (OddSocketsConnectionException ex)
{
    Console.WriteLine($"Connection error: {ex.Message}");
    // Handle connection issues
}
catch (OddSocketsChannelException ex)
{
    Console.WriteLine($"Channel error: {ex.Message}");
    // Handle channel-specific issues
}
catch (OddSocketsException ex)
{
    Console.WriteLine($"General error: {ex.Message}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    // Handle other OddSockets errors
}
```

### Custom Logging

```csharp
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

var logger = loggerFactory.CreateLogger<OddSocketsClient>();
var client = new OddSocketsClient(config, logger);
```

### Dependency Injection with Logging

```csharp
// Program.cs
builder.Services.AddLogging();
builder.Services.AddSingleton<OddSocketsConfig>(/* config */);
builder.Services.AddSingleton<OddSocketsClient>();

// Usage
public class MyService
{
    private readonly OddSocketsClient _client;
    private readonly ILogger<MyService> _logger;

    public MyService(OddSocketsClient client, ILogger<MyService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string message)
    {
        try
        {
            var channel = _client.Channel("notifications");
            await channel.PublishAsync(message);
            _logger.LogInformation("Notification sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification");
            throw;
        }
    }
}
```

## Examples

### Real-time Chat Application

```csharp
public class ChatService
{
    private readonly OddSocketsClient _client;
    private readonly OddSocketsChannel _channel;

    public ChatService(OddSocketsClient client)
    {
        _client = client;
        _channel = _client.Channel("chat-room");
    }

    public async Task StartAsync()
    {
        await _client.ConnectAsync();
        
        await _channel.SubscribeAsync(
            OnMessageReceived,
            SubscribeOptions.Builder()
                .WithPresence(true)
                .WithHistory(true)
                .Build()
        );
    }

    public async Task SendMessageAsync(string username, string message)
    {
        await _channel.PublishAsync(new
        {
            username,
            message,
            timestamp = DateTime.UtcNow
        });
    }

    private void OnMessageReceived(Message message)
    {
        // Handle incoming chat message
        Console.WriteLine($"Chat: {message.Data}");
    }
}
```

### Live Dashboard Updates

```csharp
public class DashboardService
{
    private readonly OddSocketsClient _client;

    public DashboardService(OddSocketsClient client)
    {
        _client = client;
    }

    public async Task PublishMetricsAsync(object metrics)
    {
        var channel = _client.Channel("dashboard-metrics");
        
        await channel.PublishAsync(metrics, 
            PublishOptions.Builder()
                .WithTtl(60) // Metrics expire after 1 minute
                .WithMetadata("type", "metrics")
                .Build()
        );
    }

    public async Task SubscribeToUpdatesAsync(Action<object> onUpdate)
    {
        var channel = _client.Channel("dashboard-metrics");
        
        await channel.SubscribeAsync(
            message => onUpdate(message.Data),
            SubscribeOptions.Builder()
                .WithFilter("metrics")
                .Build()
        );
    }
}
```

## Enhanced Features

Beyond core pub/sub, OddSockets ships a Slack-like **enhanced surface** — reactions,
typing indicators, threads, read receipts, presence/status, notifications, DMs,
channel management, message editing and search. It lives on `client.Enhanced`.
The pattern is always the same:

1. **Send** an action with a `client.Enhanced.*Async(...)` method (PascalCase,
   positional arguments).
2. **Receive** the paired broadcast with `client.On("<event>", data => ...)` — the
   worker forwards every enhanced broadcast onto the client's raw event surface
   (delivered as a `System.Text.Json.JsonElement`).

```csharp
using OddSockets;
using OddSockets.Models;
using System.Text.Json;

var config = new OddSocketsConfigBuilder()
    .WithApiKey("ak_live_your_api_key_here")
    .WithUserId("alice")
    .Build();

using var client = new OddSocketsClient(config);
await client.ConnectAsync();

var channel = client.Channel("room-42");
await channel.SubscribeAsync(message => { /* ... */ },
    SubscribeOptions.Builder().WithPresence(true).Build());

// Receive-path: broadcasts from other users on the channel
client.On("user_typing",    data => Console.WriteLine($"{data.GetProperty("userId")} is typing"));
client.On("reaction_added", data => Console.WriteLine($"reaction {data.GetProperty("emoji")}"));
client.On("thread_reply",   data => Console.WriteLine("new thread reply"));

// Send-path: enhanced actions over the live socket
await client.Enhanced.StartTypingAsync("alice", "room-42");
await client.Enhanced.AddReactionAsync("msg-1", "room-42", ":thumbsup:", "alice", "Alice");
await client.Enhanced.ThreadReplyAsync("room-42", "msg-1", "Replying in the thread", "alice", "Alice");
```

Each area exposes methods on `client.Enhanced`; the worker broadcasts the paired
events which you handle with `client.On(...)`. Query methods (`Get*Async`,
`Search*Async`) return a `Task<JsonElement>` that resolves with the worker response.

| Area | Requests (`client.Enhanced.*`) | Broadcast events (`client.On`) |
|------|--------------------------------|--------------------------------|
| Typing | `StartTypingAsync`, `StopTypingAsync` | `user_typing`, `user_stopped_typing` |
| Reactions | `AddReactionAsync`, `RemoveReactionAsync`, `GetReactionsAsync` | `reaction_added`, `reaction_removed` |
| Threads | `ThreadReplyAsync`, `GetThreadAsync`, `SubscribeThreadAsync`, `FollowThreadAsync`, `UnfollowThreadAsync`, `MarkThreadReadAsync` | `thread_reply`, `thread_subscribed`, `thread_followed`, `thread_read_updated` |
| Read receipts | `MarkReadAsync`, `MarkAllReadAsync`, `GetUnreadCountsAsync` | `user_read`, `unread_count_updated`, `all_marked_read` |
| Messages | `EditMessageAsync`, `DeleteMessageAsync`, `PinMessageAsync`, `UnpinMessageAsync`, `GetPinnedMessagesAsync` | `message_edited`, `message_deleted`, `message_pinned`, `message_unpinned` |
| Presence & status | `SetStatusAsync`, `SetCustomStatusAsync`, `ClearCustomStatusAsync`, `SetDNDAsync`, `ClearDNDAsync`, `GetUserPresenceAsync` | `user_status_changed`, `custom_status_updated`, `dnd_status_changed` |
| Channels | `CreateChannelAsync`, `UpdateChannelAsync`, `ArchiveChannelAsync`, `InviteToChannelAsync`, `JoinChannelAsync`, `LeaveChannelAsync`, `GetChannelMembersAsync` | `channel_created`, `channel_updated`, `user_invited`, `user_joined_channel`, `user_left_channel` |
| DMs | `CreateDMAsync`, `SendDMAsync`, `GetDMConversationsAsync` | `dm_created`, `dm_received` |
| Notifications | `SubscribeNotificationsAsync`, `GetNotificationsAsync`, `MarkNotificationReadAsync`, `ClearNotificationsAsync` | `notification`, `notification_read`, `notifications_cleared` |
| Search | `SearchMessagesAsync`, `SearchInChannelAsync`, `SearchByUserAsync`, `FilterMessagesAsync` | (query results returned via `Task<JsonElement>`) |

For any worker event not wrapped above, subscribe with the raw
`client.On("<event>", handler)` API — all enhanced broadcasts are forwarded onto
the client surface.

## API Reference

### OddSocketsClient

| Method | Description |
|--------|-------------|
| `ConnectAsync()` | Connect to OddSockets platform |
| `DisconnectAsync()` | Disconnect from platform |
| `Channel(string)` | Get or create a channel |
| `PublishBulkAsync(IEnumerable<BulkMessage>)` | Publish multiple messages |
| `On(EventType, Func<object?, Task>)` | Add event handler |
| `On(string, Action<JsonElement>)` | Add raw named-event handler (enhanced broadcasts) |
| `Off(EventType, Func<object?, Task>?)` | Remove event handler |
| `Enhanced` | Enhanced (Slack-like) feature surface |

### OddSocketsChannel

| Method | Description |
|--------|-------------|
| `SubscribeAsync(Func<Message, Task>, SubscribeOptions?)` | Subscribe to messages |
| `UnsubscribeAsync()` | Unsubscribe from messages |
| `PublishAsync(object?, PublishOptions?)` | Publish a message |
| `GetHistoryAsync(HistoryOptions?)` | Get message history |
| `GetPresenceAsync()` | Get presence information |

### Configuration Options

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ApiKey` | `string` | Required | Your OddSockets API key |
| `ManagerUrl` | `string` | `ODDSOCKETS_MANAGER_URL`, else `https://connect.oddsockets.tyga.network` | Manager service URL |
| `UserId` | `string?` | Auto-generated | User identifier |
| `AutoConnect` | `bool` | `true` | Auto-connect on creation |
| `ReconnectAttempts` | `int` | `5` | Max reconnection attempts |
| `HeartbeatInterval` | `int` | `30` | Heartbeat interval (seconds) |
| `Timeout` | `int` | `10` | Request timeout (seconds) |

## Error Handling

The SDK provides specific exception types for different error scenarios:

- `OddSocketsException` - Base exception for all SDK errors
- `OddSocketsConnectionException` - Connection-related errors
- `OddSocketsAuthenticationException` - Authentication failures
- `OddSocketsChannelException` - Channel operation errors
- `OddSocketsMessageException` - Message publishing errors

## Thread Safety

The OddSockets .NET SDK is designed to be thread-safe:

- Multiple threads can safely call methods on the same client instance
- Channel operations are protected by internal synchronization
- Event handlers are invoked safely across threads
- Concurrent publish operations are supported

## Performance Considerations

- Use bulk publishing for high-throughput scenarios
- Configure appropriate TTL values for messages
- Consider message filtering to reduce client-side processing
- Use presence tracking judiciously (only when needed)
- Implement proper connection pooling in web applications

## Compatibility

- **.NET 6.0** and later
- **.NET 8.0** and later  
- **.NET Standard 2.0** (for .NET Framework 4.6.1+)
- **ASP.NET Core** 6.0+
- **Blazor** Server and WebAssembly
- **MAUI** applications

## Get a Free API Key

AI agents can sign up with a verified email in two steps — no dashboard, no human required.

**Step 1:** Request a verification code
```bash
curl -X POST https://oddsockets.com/api/agent-signup \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "agentName": "my-agent", "platform": "csharp"}'
```

**Step 2:** Verify the 6-digit code from your email and get your API key
```bash
curl -X POST https://oddsockets.com/api/agent-signup/verify \
  -H "Content-Type: application/json" \
  -d '{"email": "you@example.com", "code": "123456", "agentName": "my-agent"}'
```

## Plans

| | Free | Starter | Pro |
|---|---|---|---|
| **Price** | $0/mo | $49.99/mo | $299/mo |
| **MAU** | 100 | 1,000 | 50,000 |
| **Concurrent connections** | 50 | 1,000 | Unlimited |
| **Messages/day** | 10,000 | 4,320,000 | Unlimited |
| **Channels** | 10 | Unlimited | Unlimited |
| **Storage** | 100MB (24h) | 50GB (6 months) | Unlimited |

All limits are enforced in real time.

## Get Accredited

<a href="https://tyga.games/accreditation"><img src="https://prodmedia.tyga.host/public/tyga.cloud/landing/tyga.games/tygagames-black-words.svg" alt="tyga.games accreditation" height="44"></a>

Prove you can build and operate real-time features on OddSockets — channels, presence, pub/sub, delivery guarantees and production liveops — on the stack itself. Three tiers (**TCU / TCA / TCP**), certified through **tyga.games** and delivered on ClassaaS.

[**Get accredited on tyga.games →**](https://tyga.games/accreditation)

## Support

- [Documentation](https://docs.oddsockets.com/sdks/csharp)
- [Issue Tracker](https://github.com/jyswee/oddsockets-csharp-sdk/issues)
- [Email Support](mailto:support@oddsockets.com)

## License

MIT License - Copyright (c) 2026 Joe Wee, Tyga.Cloud Ltd. See [LICENSE](LICENSE) for details.
