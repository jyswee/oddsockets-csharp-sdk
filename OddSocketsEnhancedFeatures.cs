using System.Text.Json;
using OddSockets.Models;
using OddSockets.Exceptions;

namespace OddSockets;

/// <summary>
/// Enhanced (Slack-like) feature surface for the OddSockets .NET SDK.
///
/// Covers threads, reactions, read receipts, channels, direct messages,
/// notifications, presence/typing, message editing and search. Every method
/// travels over the real Socket.IO connection to the assigned worker: void
/// methods emit a fire-and-forget event, and Task-returning methods emit a
/// request and await the correlated response event. Broadcasts (for example
/// "user_typing" and "reaction_added") arrive on the client's raw event
/// surface via <see cref="OddSocketsClient.On(string, Action{JsonElement})"/>.
/// </summary>
public class OddSocketsEnhancedFeatures
{
    private const int TimeoutMs = 10000;

    private readonly OddSocketsClient _client;

    internal OddSocketsEnhancedFeatures(OddSocketsClient client)
    {
        _client = client;
    }

    // ==================== THREAD EVENTS ====================

    /// <summary>Reply to a message in a thread.</summary>
    public Task<JsonElement> ThreadReplyAsync(string channel, string parentMessageId, string message, string userId, string userName)
        => RequestAsync("thread_reply", new Dictionary<string, object>
        {
            ["channel"] = channel,
            ["parentMessageId"] = parentMessageId,
            ["message"] = message,
            ["userId"] = userId,
            ["userName"] = userName
        }, "thread_reply_success");

    /// <summary>Get a thread with all of its replies.</summary>
    public Task<JsonElement> GetThreadAsync(string threadId)
        => RequestAsync("get_thread", new Dictionary<string, object> { ["threadId"] = threadId }, "thread_data");

    /// <summary>Subscribe to updates for a thread.</summary>
    public Task<JsonElement> SubscribeThreadAsync(string threadId, string userId)
        => RequestAsync("subscribe_thread", new Dictionary<string, object>
        {
            ["threadId"] = threadId,
            ["userId"] = userId
        }, "thread_subscribed");

    /// <summary>Mark a thread as read.</summary>
    public Task MarkThreadReadAsync(string threadId, string userId)
        => Emit("mark_thread_read", new Dictionary<string, object> { ["threadId"] = threadId, ["userId"] = userId });

    /// <summary>Follow a thread.</summary>
    public Task FollowThreadAsync(string threadId, string userId)
        => Emit("follow_thread", new Dictionary<string, object> { ["threadId"] = threadId, ["userId"] = userId });

    /// <summary>Unfollow a thread.</summary>
    public Task UnfollowThreadAsync(string threadId, string userId)
        => Emit("unfollow_thread", new Dictionary<string, object> { ["threadId"] = threadId, ["userId"] = userId });

    // ==================== REACTION EVENTS ====================

    /// <summary>Add a reaction to a message.</summary>
    public Task AddReactionAsync(string messageId, string channel, string emoji, string userId, string userName)
        => Emit("add_reaction", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["emoji"] = emoji,
            ["userId"] = userId,
            ["userName"] = userName
        });

    /// <summary>Remove a reaction from a message.</summary>
    public Task RemoveReactionAsync(string messageId, string channel, string emoji, string userId)
        => Emit("remove_reaction", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["emoji"] = emoji,
            ["userId"] = userId
        });

    /// <summary>Get all reactions for a message.</summary>
    public Task<JsonElement> GetReactionsAsync(string messageId)
        => RequestAsync("get_reactions", new Dictionary<string, object> { ["messageId"] = messageId }, "message_reactions");

    // ==================== READ RECEIPT EVENTS ====================

    /// <summary>Mark a message as read.</summary>
    public Task MarkReadAsync(string messageId, string channel, string userId, string userName)
        => Emit("mark_read", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["userId"] = userId,
            ["userName"] = userName
        });

    /// <summary>Get unread counts for a set of channels.</summary>
    public Task<JsonElement> GetUnreadCountsAsync(string userId, IEnumerable<string> channels)
        => RequestAsync("get_unread_counts", new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["channels"] = channels.ToList()
        }, "unread_counts");

    /// <summary>Mark all messages in a channel as read.</summary>
    public Task MarkAllReadAsync(string channel, string userId)
        => Emit("mark_all_read", new Dictionary<string, object> { ["channel"] = channel, ["userId"] = userId });

    // ==================== CHANNEL EVENTS ====================

    /// <summary>Create a new channel.</summary>
    public Task<JsonElement> CreateChannelAsync(string name, string type, string description, string topic, string createdBy, string createdByName)
        => RequestAsync("create_channel", new Dictionary<string, object>
        {
            ["name"] = name,
            ["type"] = type,
            ["description"] = description,
            ["topic"] = topic,
            ["createdBy"] = createdBy,
            ["createdByName"] = createdByName,
            ["members"] = new List<string>()
        }, "channel_create_success");

    /// <summary>Update channel details.</summary>
    public Task UpdateChannelAsync(string channelId, Dictionary<string, object> updates, string userId)
        => Emit("update_channel", new Dictionary<string, object>
        {
            ["channelId"] = channelId,
            ["updates"] = updates,
            ["userId"] = userId
        });

    /// <summary>Archive a channel.</summary>
    public Task ArchiveChannelAsync(string channelId, string userId)
        => Emit("archive_channel", new Dictionary<string, object> { ["channelId"] = channelId, ["userId"] = userId });

    /// <summary>Invite a user to a channel.</summary>
    public Task InviteToChannelAsync(string channelId, string invitedUserId, string invitedUserName, string invitedBy)
        => Emit("invite_to_channel", new Dictionary<string, object>
        {
            ["channelId"] = channelId,
            ["invitedUserId"] = invitedUserId,
            ["invitedUserName"] = invitedUserName,
            ["invitedBy"] = invitedBy
        });

    /// <summary>Remove a user from a channel.</summary>
    public Task RemoveFromChannelAsync(string channelId, string removedUserId, string removedBy)
        => Emit("remove_from_channel", new Dictionary<string, object>
        {
            ["channelId"] = channelId,
            ["removedUserId"] = removedUserId,
            ["removedBy"] = removedBy
        });

    /// <summary>Join a public channel.</summary>
    public Task JoinChannelAsync(string channelId, string userId, string userName)
        => Emit("join_channel", new Dictionary<string, object>
        {
            ["channelId"] = channelId,
            ["userId"] = userId,
            ["userName"] = userName
        });

    /// <summary>Leave a channel.</summary>
    public Task LeaveChannelAsync(string channelId, string userId)
        => Emit("leave_channel", new Dictionary<string, object> { ["channelId"] = channelId, ["userId"] = userId });

    /// <summary>Get the members of a channel.</summary>
    public Task<JsonElement> GetChannelMembersAsync(string channelId)
        => RequestAsync("get_channel_members", new Dictionary<string, object> { ["channelId"] = channelId }, "channel_members");

    // ==================== DIRECT MESSAGE EVENTS ====================

    /// <summary>Create or fetch a direct-message conversation.</summary>
    public Task<JsonElement> CreateDMAsync(IEnumerable<string> userIds, string type)
        => RequestAsync("create_dm", new Dictionary<string, object>
        {
            ["userIds"] = userIds.ToList(),
            ["type"] = type
        }, "dm_create_success");

    /// <summary>Send a direct message.</summary>
    public Task SendDMAsync(string conversationId, string message, string userId, string userName)
        => Emit("send_dm", new Dictionary<string, object>
        {
            ["conversationId"] = conversationId,
            ["message"] = message,
            ["userId"] = userId,
            ["userName"] = userName
        });

    /// <summary>Get a user's direct-message conversations.</summary>
    public Task<JsonElement> GetDMConversationsAsync(string userId, bool includeArchived)
        => RequestAsync("get_dm_conversations", new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["includeArchived"] = includeArchived
        }, "dm_conversations");

    // ==================== NOTIFICATION EVENTS ====================

    /// <summary>Subscribe to a user's notifications.</summary>
    public Task SubscribeNotificationsAsync(string userId)
        => Emit("subscribe_notifications", new Dictionary<string, object> { ["userId"] = userId });

    /// <summary>Mark a notification as read.</summary>
    public Task MarkNotificationReadAsync(string notificationId, string userId)
        => Emit("mark_notification_read", new Dictionary<string, object>
        {
            ["notificationId"] = notificationId,
            ["userId"] = userId
        });

    /// <summary>Mark all notifications as read.</summary>
    public Task MarkAllNotificationsReadAsync(string userId)
        => Emit("mark_all_notifications_read", new Dictionary<string, object> { ["userId"] = userId });

    /// <summary>Clear all notifications.</summary>
    public Task ClearNotificationsAsync(string userId)
        => Emit("clear_notifications", new Dictionary<string, object> { ["userId"] = userId });

    /// <summary>Get a user's notifications.</summary>
    public Task<JsonElement> GetNotificationsAsync(string userId, int limit, string status)
        => RequestAsync("get_notifications", new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["limit"] = limit,
            ["status"] = status
        }, "notifications_data");

    // ==================== PRESENCE EVENTS ====================

    /// <summary>Set a user's status.</summary>
    public Task SetStatusAsync(string userId, string status)
        => Emit("set_status", new Dictionary<string, object> { ["userId"] = userId, ["status"] = status });

    /// <summary>Set a user's custom status.</summary>
    public Task SetCustomStatusAsync(string userId, string emoji, string text, string? expiresAt = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["userId"] = userId,
            ["emoji"] = emoji,
            ["text"] = text
        };
        if (expiresAt != null) payload["expiresAt"] = expiresAt;
        return Emit("set_custom_status", payload);
    }

    /// <summary>Clear a user's custom status.</summary>
    public Task ClearCustomStatusAsync(string userId)
        => Emit("clear_custom_status", new Dictionary<string, object> { ["userId"] = userId });

    /// <summary>Enable Do Not Disturb for a user.</summary>
    public Task SetDNDAsync(string userId, string? until = null)
    {
        var payload = new Dictionary<string, object> { ["userId"] = userId };
        if (until != null) payload["until"] = until;
        return Emit("set_dnd", payload);
    }

    /// <summary>Disable Do Not Disturb for a user.</summary>
    public Task ClearDNDAsync(string userId)
        => Emit("clear_dnd", new Dictionary<string, object> { ["userId"] = userId });

    /// <summary>Start a typing indicator on a channel.</summary>
    public Task StartTypingAsync(string userId, string channel)
        => Emit("start_typing", new Dictionary<string, object> { ["userId"] = userId, ["channel"] = channel });

    /// <summary>Stop a typing indicator on a channel.</summary>
    public Task StopTypingAsync(string userId, string channel)
        => Emit("stop_typing", new Dictionary<string, object> { ["userId"] = userId, ["channel"] = channel });

    /// <summary>Get presence information for a set of users.</summary>
    public Task<JsonElement> GetUserPresenceAsync(IEnumerable<string> userIds)
        => RequestAsync("get_user_presence", new Dictionary<string, object> { ["userIds"] = userIds.ToList() }, "user_presence_data");

    // ==================== MESSAGE EDITING EVENTS ====================

    /// <summary>Edit a message.</summary>
    public Task EditMessageAsync(string messageId, string channel, string newContent, string userId)
        => Emit("edit_message", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["newContent"] = newContent,
            ["userId"] = userId
        });

    /// <summary>Delete a message.</summary>
    public Task DeleteMessageAsync(string messageId, string channel, string userId)
        => Emit("delete_message", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["userId"] = userId
        });

    /// <summary>Pin a message to a channel.</summary>
    public Task PinMessageAsync(string messageId, string channel, string userId)
        => Emit("pin_message", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["userId"] = userId
        });

    /// <summary>Unpin a message from a channel.</summary>
    public Task UnpinMessageAsync(string messageId, string channel, string userId)
        => Emit("unpin_message", new Dictionary<string, object>
        {
            ["messageId"] = messageId,
            ["channel"] = channel,
            ["userId"] = userId
        });

    /// <summary>Get the pinned messages in a channel.</summary>
    public Task<JsonElement> GetPinnedMessagesAsync(string channel)
        => RequestAsync("get_pinned_messages", new Dictionary<string, object> { ["channel"] = channel }, "pinned_messages");

    // ==================== SEARCH EVENTS ====================

    /// <summary>Search messages across all channels.</summary>
    public Task<JsonElement> SearchMessagesAsync(string query, string userId, int limit)
        => RequestAsync("search_messages", new Dictionary<string, object>
        {
            ["query"] = query,
            ["userId"] = userId,
            ["limit"] = limit
        }, "search_results");

    /// <summary>Filter messages by arbitrary criteria.</summary>
    public Task<JsonElement> FilterMessagesAsync(Dictionary<string, object> filters)
        => RequestAsync("filter_messages", filters, "filter_results");

    /// <summary>Search within a specific channel.</summary>
    public Task<JsonElement> SearchInChannelAsync(string channel, string query, int limit)
        => RequestAsync("search_in_channel", new Dictionary<string, object>
        {
            ["channel"] = channel,
            ["query"] = query,
            ["limit"] = limit
        }, "channel_search_results");

    /// <summary>Search messages by a specific user.</summary>
    public Task<JsonElement> SearchByUserAsync(string userId, string? query, int limit)
    {
        var payload = new Dictionary<string, object> { ["userId"] = userId, ["limit"] = limit };
        if (query != null) payload["query"] = query;
        return RequestAsync("search_by_user", payload, "user_search_results");
    }

    // ==================== INTERNALS ====================

    private Task Emit(string eventName, Dictionary<string, object> payload)
    {
        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);
        return _client.EmitAsync(eventName, payload);
    }

    private async Task<JsonElement> RequestAsync(string emitEvent, Dictionary<string, object> payload, string successEvent)
    {
        if (!_client.IsConnected)
            throw new OddSocketsConnectionException("Not connected to OddSockets", ErrorCodes.ConnectionFailed);

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        _client.Once(successEvent, data => tcs.TrySetResult(data));
        _client.Once("error", data =>
        {
            if (data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("event", out var ev) &&
                ev.ValueKind == JsonValueKind.String &&
                ev.GetString() == emitEvent)
            {
                var message = data.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                    ? m.GetString() ?? "Enhanced request failed"
                    : "Enhanced request failed";
                tcs.TrySetException(new OddSocketsMessageException(message, ErrorCodes.MessageDeliveryFailed, emitEvent));
            }
        });

        await _client.EmitAsync(emitEvent, payload);

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeoutMs));
        if (completed != tcs.Task)
            throw new OddSocketsConnectionException($"Request timed out: {emitEvent}", ErrorCodes.OperationTimeout);

        return await tcs.Task;
    }
}
