using System.Text.Json;
using OddSockets;
using OddSockets.Models;

namespace OddSockets.Demo;

/// <summary>
/// OddSockets .NET SDK - runnable two-client demo.
///
/// Two genuine end-to-end round-trips, each using TWO independent clients:
///   1. Core pub/sub: a SUBSCRIBER ("alice") receives a message a PUBLISHER
///      ("bob") sends on its own connection.
///   2. Enhanced events: bob fires enhanced.StartTyping + enhanced.AddReaction
///      and alice receives "user_typing" + "reaction_added" on her public raw
///      event surface (client.On).
///
/// Because the two clients are separate connections, anything alice receives can
/// ONLY have travelled through the OddSockets worker - it cannot be a local
/// echo. Uses the SAME SDK a consumer installs. No mocks.
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        var apiKey = Environment.GetEnvironmentVariable("ODDSOCKETS_API_KEY");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.Error.WriteLine("Missing ODDSOCKETS_API_KEY. Get a free key (see README), then:");
            Console.Error.WriteLine("  export ODDSOCKETS_API_KEY=\"ak_...\"");
            return 1;
        }

        if (!await BasicRoundTripAsync(apiKey)) return 2;
        Console.WriteLine();
        if (!await EnhancedRoundTripAsync(apiKey)) return 3;

        return 0;
    }

    // ---- Scenario 1: core pub/sub cross-client round-trip -------------------
    private static async Task<bool> BasicRoundTripAsync(string apiKey)
    {
        var nonce = Guid.NewGuid().ToString("N");
        var channelName = "demo-" + Guid.NewGuid().ToString("N").Substring(0, 10);
        var verified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var subscriber = new OddSocketsClient(new OddSocketsConfigBuilder()
            .WithApiKey(apiKey).WithUserId("alice").WithAutoConnect(false).Build());
        var publisher = new OddSocketsClient(new OddSocketsConfigBuilder()
            .WithApiKey(apiKey).WithUserId("bob").WithAutoConnect(false).Build());

        subscriber.On(EventType.Error, d => Console.Error.WriteLine($"[alice] error {d}"));
        publisher.On(EventType.Error, d => Console.Error.WriteLine($"[bob]   error {d}"));

        Console.WriteLine("[connect] connecting both clients...");
        await Task.WhenAll(subscriber.ConnectAsync(), publisher.ConnectAsync());
        Console.WriteLine($"[connect] alice = {subscriber.IsConnected}, bob = {publisher.IsConnected}");

        var inbox = subscriber.Channel(channelName);
        await inbox.SubscribeAsync(async (Message msg) =>
        {
            if (NonceOf(msg.Data) != nonce) return;
            Console.WriteLine("[alice] received bob's message (nonce matched) - real round-trip.");
            try
            {
                var presence = await inbox.GetPresenceAsync();
                Console.WriteLine($"[alice] presence: {presence.Count} user(s).");
                await inbox.UnsubscribeAsync();
                Console.WriteLine("[alice] unsubscribed.");
            }
            catch { /* best-effort; round-trip already proven */ }
            verified.TrySetResult(true);
        }, new SubscribeOptions { EnablePresence = true });
        Console.WriteLine($"[alice] subscribed to {channelName} (presence on)");

        var outbox = publisher.Channel(channelName);
        var ack = await outbox.PublishAsync(new { text = "hello from bob", nonce, from = "bob" });
        Console.WriteLine($"[bob] published, ack = {{ messageId = {ack.MessageId}, channel = {ack.Channel} }}");

        var completed = await Task.WhenAny(verified.Task, Task.Delay(TimeSpan.FromSeconds(15)));
        bool ok = completed == verified.Task;
        Console.WriteLine(ok
            ? $"\nOK - cross-client round-trip verified on {channelName}"
            : "\nTIMEOUT - no cross-client delivery within 15s");

        await subscriber.DisconnectAsync();
        await publisher.DisconnectAsync();
        return ok;
    }

    // ---- Scenario 2: enhanced-events cross-client receive-path --------------
    private static async Task<bool> EnhancedRoundTripAsync(string apiKey)
    {
        var channelName = "enh-" + Guid.NewGuid().ToString("N").Substring(0, 10);
        var typingSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reactionSeen = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var subscriber = new OddSocketsClient(new OddSocketsConfigBuilder()
            .WithApiKey(apiKey).WithUserId("alice").WithAutoConnect(false).Build());
        var publisher = new OddSocketsClient(new OddSocketsConfigBuilder()
            .WithApiKey(apiKey).WithUserId("bob").WithAutoConnect(false).Build());

        subscriber.On(EventType.Error, d => Console.Error.WriteLine($"[alice] error {d}"));
        publisher.On(EventType.Error, d => Console.Error.WriteLine($"[bob]   error {d}"));

        // alice listens on her PUBLIC raw event surface - these can only fire if
        // the broadcast crossed the worker from bob's separate connection.
        subscriber.On("user_typing", data =>
        {
            if (StringProp(data, "userId") == "bob")
            {
                Console.WriteLine("[alice] received 'user_typing' from bob - broadcast round-trip.");
                typingSeen.TrySetResult(true);
            }
        });
        subscriber.On("reaction_added", data =>
        {
            Console.WriteLine("[alice] received 'reaction_added' (:thumbsup:) from bob - broadcast round-trip.");
            reactionSeen.TrySetResult(true);
        });

        Console.WriteLine("[connect] connecting both clients...");
        await Task.WhenAll(subscriber.ConnectAsync(), publisher.ConnectAsync());

        var aliceRoom = subscriber.Channel(channelName);
        var bobRoom = publisher.Channel(channelName);
        await aliceRoom.SubscribeAsync((Message _) => { }, new SubscribeOptions { EnablePresence = true });
        await bobRoom.SubscribeAsync((Message _) => { }, new SubscribeOptions { EnablePresence = true });
        Console.WriteLine($"[both] subscribed to {channelName}");

        // bob publishes a message so there is a real messageId to react to.
        var ack = await bobRoom.PublishAsync(new { text = "reactable" });
        Console.WriteLine($"[bob] published messageId={ack.MessageId}");

        Console.WriteLine("[bob] enhanced.StartTyping(bob) ...");
        await publisher.Enhanced.StartTypingAsync("bob", channelName);

        Console.WriteLine("[bob] enhanced.AddReaction :thumbsup: ...");
        await publisher.Enhanced.AddReactionAsync(ack.MessageId, channelName, ":thumbsup:", "bob", "Bob");

        var both = Task.WhenAll(typingSeen.Task, reactionSeen.Task);
        var completed = await Task.WhenAny(both, Task.Delay(TimeSpan.FromSeconds(15)));
        bool ok = completed == both;
        Console.WriteLine(ok
            ? "\nOK - enhanced broadcast receive-path verified (user_typing + reaction_added)"
            : "\nTIMEOUT - enhanced broadcasts not received within 15s");

        await subscriber.DisconnectAsync();
        await publisher.DisconnectAsync();
        return ok;
    }

    // The message payload arrives as Message.Data; pull the nonce out robustly.
    private static string? NonceOf(object? data)
    {
        if (data is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("nonce", out var n)) return n.GetString();
                foreach (var key in new[] { "message", "data" })
                    if (root.TryGetProperty(key, out var inner) &&
                        inner.ValueKind == JsonValueKind.Object &&
                        inner.TryGetProperty("nonce", out var n2)) return n2.GetString();
            }
        }
        catch { /* not JSON we recognise */ }
        return null;
    }

    private static string? StringProp(JsonElement data, string name)
        => data.ValueKind == JsonValueKind.Object &&
           data.TryGetProperty(name, out var v) &&
           v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;
}
