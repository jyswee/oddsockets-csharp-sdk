using Microsoft.Extensions.Logging;
using OddSockets;
using OddSockets.Models;

namespace OddSockets.Examples;

/// <summary>
/// Basic OddSockets SDK Usage Example
/// 
/// This example demonstrates the core functionality of the OddSockets .NET SDK:
/// - Connecting to the platform
/// - Subscribing to channels
/// - Publishing messages
/// - Bulk publishing
/// - Handling events
/// </summary>
public class BasicUsage
{
    private static readonly ILogger<BasicUsage> Logger = LoggerFactory
        .Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information))
        .CreateLogger<BasicUsage>();

    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Starting OddSockets .NET SDK Basic Example");

        try
        {
            await RunExampleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Example failed: {ex.Message}");
            Logger.LogError(ex, "Example execution failed");
        }

        Console.WriteLine("👋 Example completed! Press any key to exit...");
        Console.ReadKey();
    }

    private static async Task RunExampleAsync()
    {
        // Create OddSockets client using builder pattern
        var config = new OddSocketsConfigBuilder()
            .WithApiKey("ak_live_1234567890abcdef") // Replace with your API key
            .WithManagerUrl("https://manager1.oddsockets.tyga.network") // Optional: defaults to this
            .WithUserId("user123") // Optional: defaults to auto-generated
            .WithAutoConnect(false) // We'll connect manually for this example
            .WithReconnectAttempts(3)
            .WithHeartbeatInterval(30)
            .Build();

        using var client = new OddSocketsClient(config, Logger);

        // Listen for connection events
        client.On(EventType.Connected, data =>
        {
            Console.WriteLine("✅ Connected to OddSockets!");
            Logger.LogInformation("Connected to OddSockets: {Data}", data);
        });

        client.On(EventType.WorkerAssigned, data =>
        {
            Console.WriteLine($"🎯 Assigned to worker: {client.WorkerInfo.WorkerId}");
            Logger.LogInformation("Worker assigned: {Data}", data);
        });

        client.On(EventType.Error, data =>
        {
            Console.WriteLine($"❌ Connection error: {data}");
            Logger.LogError("Connection error: {Error}", data);
        });

        client.On(EventType.Disconnected, data =>
        {
            Console.WriteLine($"🔌 Disconnected: {data}");
            Logger.LogInformation("Disconnected: {Reason}", data);
        });

        // Connect to OddSockets
        Console.WriteLine("🔄 Connecting to OddSockets...");
        await client.ConnectAsync();

        // Wait a moment for connection to establish
        await Task.Delay(1000);

        if (!client.IsConnected)
        {
            throw new InvalidOperationException("Failed to connect to OddSockets");
        }

        // Get a channel
        var channel = client.Channel("demo-channel");

        // Subscribe to the channel
        Console.WriteLine("📡 Subscribing to demo-channel...");
        await channel.SubscribeAsync(
            message =>
            {
                Console.WriteLine($"📨 Received message: {message.Data}");
                Console.WriteLine($"   From: {message.UserId}");
                Console.WriteLine($"   Time: {message.Timestamp:HH:mm:ss}");
                Logger.LogInformation("Message received: {Message}", message.Data);
            },
            SubscribeOptions.Builder()
                .WithPresence(true) // Enable presence tracking
                .WithHistory(true)  // Keep message history
                .Build()
        );

        Console.WriteLine("✅ Subscribed to demo-channel");

        // Listen for presence events
        channel.On(EventType.Presence, data =>
        {
            Console.WriteLine($"👤 Presence update: {data}");
            Logger.LogInformation("Presence update: {Data}", data);
        });

        // Publish some messages
        Console.WriteLine("📤 Publishing messages...");

        // Simple text message
        var result1 = await channel.PublishAsync("Hello, OddSockets!");
        Console.WriteLine($"✅ Published message: {result1.MessageId}");

        // Object message
        var result2 = await channel.PublishAsync(new
        {
            type = "notification",
            title = "Welcome!",
            body = "Thanks for trying OddSockets .NET SDK",
            data = new { userId = "user123", timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        });
        Console.WriteLine($"✅ Published object message: {result2.MessageId}");

        // Message with options
        var result3 = await channel.PublishAsync(
            "This message has metadata",
            PublishOptions.Builder()
                .WithTtl(3600) // 1 hour TTL
                .WithMetadata("priority", "high")
                .WithMetadata("category", "demo")
                .WithHistory(true)
                .Build()
        );
        Console.WriteLine($"✅ Published message with options: {result3.MessageId}");

        // Test bulk publishing
        Console.WriteLine("📦 Testing bulk publishing...");
        var bulkMessages = new List<BulkMessage>
        {
            new("demo-channel", "Bulk message 1"),
            new("demo-channel", "Bulk message 2"),
            new("demo-channel", new { type = "bulk", content = "Bulk message 3" })
        };

        var bulkResults = await client.PublishBulkAsync(bulkMessages);
        var successful = bulkResults.Count(r => r.Success);
        Console.WriteLine($"✅ Bulk published {successful}/{bulkMessages.Count} messages successfully");

        foreach (var (result, index) in bulkResults.Select((r, i) => (r, i)))
        {
            if (result.Success)
            {
                Console.WriteLine($"   Message {index + 1}: ✅ {result.Result?.MessageId}");
            }
            else
            {
                Console.WriteLine($"   Message {index + 1}: ❌ {result.Error}");
            }
        }

        // Wait for messages to be processed
        await Task.Delay(2000);

        // Get message history
        Console.WriteLine("📚 Fetching message history...");
        var history = await channel.GetHistoryAsync(
            HistoryOptions.Builder()
                .WithLimit(10)
                .Build()
        );
        Console.WriteLine($"📖 Found {history.Count} messages in history");

        foreach (var msg in history.Take(3))
        {
            Console.WriteLine($"   {msg.Timestamp:HH:mm:ss} - {msg.Data}");
        }

        // Get presence information
        Console.WriteLine("👥 Fetching presence information...");
        var presence = await channel.GetPresenceAsync();
        Console.WriteLine($"👤 {presence.Count} users in channel");

        foreach (var user in presence.Users)
        {
            Console.WriteLine($"   - {user}");
        }

        // Demonstrate error handling
        Console.WriteLine("🔧 Testing error handling...");
        try
        {
            // Try to publish to an invalid channel
            var invalidChannel = client.Channel("");
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"✅ Caught expected error: {ex.Message}");
        }

        // Keep the example running for a bit to see events
        Console.WriteLine("⏳ Keeping connection alive for 10 seconds...");
        await Task.Delay(10000);

        // Unsubscribe and disconnect
        Console.WriteLine("🔌 Unsubscribing and disconnecting...");
        await channel.UnsubscribeAsync();
        await client.DisconnectAsync();

        Console.WriteLine("✅ Example completed successfully!");
    }
}

/// <summary>
/// Advanced usage example demonstrating dependency injection and ASP.NET Core integration.
/// </summary>
public class AdvancedUsage
{
    public static async Task RunWithDependencyInjectionAsync()
    {
        // Example of how to use OddSockets with dependency injection
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .AddHttpClient()
            .AddSingleton<OddSocketsConfig>(provider =>
                new OddSocketsConfigBuilder()
                    .WithApiKey("ak_live_1234567890abcdef")
                    .WithUserId("advanced-user")
                    .Build())
            .AddSingleton<OddSocketsClient>()
            .BuildServiceProvider();

        var client = services.GetRequiredService<OddSocketsClient>();
        var logger = services.GetRequiredService<ILogger<AdvancedUsage>>();

        logger.LogInformation("Starting advanced usage example");

        try
        {
            await client.ConnectAsync();

            var channel = client.Channel("advanced-channel");
            await channel.SubscribeAsync(message =>
            {
                logger.LogInformation("Advanced message received: {Data}", message.Data);
            });

            await channel.PublishAsync("Advanced message from DI example");

            await Task.Delay(2000);
            await client.DisconnectAsync();

            logger.LogInformation("Advanced example completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Advanced example failed");
        }
    }
}

/// <summary>
/// Example demonstrating real-time chat functionality.
/// </summary>
public class ChatExample
{
    public static async Task RunChatExampleAsync()
    {
        var config = new OddSocketsConfigBuilder()
            .WithApiKey("ak_live_1234567890abcdef")
            .WithUserId($"chat-user-{Guid.NewGuid():N}")
            .Build();

        using var client = new OddSocketsClient(config);

        await client.ConnectAsync();

        var chatChannel = client.Channel("chat-room");

        // Subscribe to chat messages
        await chatChannel.SubscribeAsync(
            message =>
            {
                if (message.Data is JsonElement jsonElement)
                {
                    var messageText = jsonElement.GetProperty("text").GetString();
                    var username = jsonElement.GetProperty("username").GetString();
                    Console.WriteLine($"[{message.Timestamp:HH:mm:ss}] {username}: {messageText}");
                }
            },
            SubscribeOptions.Builder()
                .WithPresence(true)
                .WithHistory(true)
                .Build()
        );

        // Simulate sending chat messages
        var messages = new[]
        {
            "Hello everyone!",
            "How is everyone doing?",
            "This .NET SDK is awesome! 🚀"
        };

        foreach (var msg in messages)
        {
            await chatChannel.PublishAsync(new
            {
                text = msg,
                username = client.UserId,
                timestamp = DateTime.UtcNow
            });

            await Task.Delay(1000);
        }

        await Task.Delay(3000);
        await client.DisconnectAsync();
    }
}
