# OddSockets C# Demo

A runnable console program that performs two genuine end-to-end round-trips against the live OddSockets platform, each using **two independent clients** (alice = subscriber, bob = publisher) on separate connections. Because the connections are separate, anything alice receives can only have travelled through the OddSockets worker - it is never a local echo.

1. **Core pub/sub**: bob publishes a nonce-tagged message; alice receives it on her own connection, reads live presence, and unsubscribes.
2. **Enhanced events**: bob fires `enhanced.StartTyping` and `enhanced.AddReaction`; alice receives `user_typing` and `reaction_added` on her public raw event surface (`client.On`) - proving the enhanced (Slack-like) surface is wired to the real Socket.IO transport.

## Get a free API key

Sign up in two steps (no card required). First request a code:

```bash
curl -X POST https://oddsockets.com/api/agent-signup \
  -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com"}'
```

Then verify the code that was emailed to you:

```bash
curl -X POST https://oddsockets.com/api/agent-signup/verify \
  -H 'Content-Type: application/json' \
  -d '{"email":"you@example.com","code":"123456"}'
```

The verify response contains your API key (starts with `ak_`).

## Run it

With a local .NET 8 SDK:

```bash
export ODDSOCKETS_API_KEY=ak_...
cd demo
dotnet run
```

Or fully containerised (no local .NET needed), from the repo root:

```bash
docker build -f demo/Dockerfile -t oddsockets-csharp-demo .
docker run --rm -e ODDSOCKETS_API_KEY="ak_..." oddsockets-csharp-demo
```

The demo reads the key from the `ODDSOCKETS_API_KEY` environment variable and never hardcodes it. On success it prints `OK - cross-client round-trip verified` and `OK - enhanced broadcast receive-path verified`, then exits 0; if either round-trip does not complete within 15 seconds it exits non-zero.

## Files

- `Program.cs` - the two-scenario, two-client demo.
- `Dockerfile` - builds the SDK from source and runs the demo against the live platform.
- `PROOF.txt` - captured console output from a live run through worker `w002-oddsockets-1`.
