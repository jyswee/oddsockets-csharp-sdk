using System.Text.Json;
using OddSockets.Models;
using OddSockets.Exceptions;

namespace OddSockets;

/// <summary>
/// Enhanced (Slack-like) feature surface for the OddSockets .NET SDK.
///
/// Covers threads, reactions, read receipts, channels, direct messages,
/// notifications, presence/typing, message editing, search and the
/// server-authoritative challenge / leaderboard / achievement lifecycle. Every
/// method travels over the real Socket.IO connection to the assigned worker:
/// void methods emit a fire-and-forget event, and Task-returning methods emit a
/// request and await the correlated response event. Broadcasts (for example
/// "user_typing" and "reaction_added") arrive on the client's raw event
/// surface via <see cref="OddSocketsClient.On(string, Action{JsonElement})"/>.
///
/// Challenge / leaderboard / achievement broadcasts that peers receive on that
/// same raw surface are: "challenge_progress", "leaderboard_rank_change",
/// "challenge_complete", "achievement_unlock", "achievement_progress",
/// "challenge_invited", "challenge_reply_received" and
/// "challenge_invite_cancelled" — subscribe with, e.g.,
/// <c>client.On("leaderboard_rank_change", ...)</c>.
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

    // ==================== CHALLENGE / LEADERBOARD EVENTS ====================
    // Server-authoritative challenge lifecycle. Progress and completions land on
    // the shared room envelope so every member (and any partner resultWebhookUrl)
    // sees challenge_progress / leaderboard_rank_change / challenge_complete /
    // achievement_unlock — subscribe with client.On("leaderboard_rank_change", ...).

    /// <summary>Create (register) a challenge run and its optional result-webhook target.</summary>
    public Task<JsonElement> CreateChallengeAsync(string challengeId, string metric, bool? ranked = null,
        string? channel = null, string? resultWebhookUrl = null, string? standingsUrl = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["challengeId"] = challengeId,
            ["metric"] = metric
        };
        if (ranked != null) payload["ranked"] = ranked.Value;
        if (channel != null) payload["channel"] = channel;
        if (resultWebhookUrl != null) payload["resultWebhookUrl"] = resultWebhookUrl;
        if (standingsUrl != null) payload["standingsUrl"] = standingsUrl;
        return RequestAsync("challenge_create", payload, "challenge_create_success");
    }

    /// <summary>Report a progress value for the connected player. Fire-and-forget.</summary>
    public Task ReportProgressAsync(string challengeId, double value, string? metric = null,
        string? eventId = null, string? cohort = null, string? platform = null, string? channel = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["challengeId"] = challengeId,
            ["value"] = value
        };
        if (metric != null) payload["metric"] = metric;
        if (eventId != null) payload["eventId"] = eventId;
        if (cohort != null) payload["cohort"] = cohort;
        if (platform != null) payload["platform"] = platform;
        if (channel != null) payload["channel"] = channel;
        return Emit("challenge_progress", payload);
    }

    /// <summary>Complete the connected player's run. Resolves with the server-authoritative result.</summary>
    public Task<JsonElement> CompleteChallengeAsync(string challengeId, string outcome,
        string? eventId = null, object? reward = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["challengeId"] = challengeId,
            ["outcome"] = outcome
        };
        if (eventId != null) payload["eventId"] = eventId;
        if (reward != null) payload["reward"] = reward;
        return RequestAsync("challenge_complete", payload, "challenge_complete_success");
    }

    /// <summary>
    /// Report achievement progress or unlock. Fire-and-forget. Pass percentComplete
    /// (0-100) for progressive achievements: &lt;100 broadcasts achievement_progress;
    /// &gt;=100 or omitted broadcasts achievement_unlock.
    /// </summary>
    public Task UnlockAchievementAsync(string achievementId, string? name = null, string? tier = null,
        double? percentComplete = null, string? challengeId = null, string? channel = null)
    {
        var payload = new Dictionary<string, object> { ["achievementId"] = achievementId };
        if (name != null) payload["name"] = name;
        if (tier != null) payload["tier"] = tier;
        if (percentComplete != null) payload["percentComplete"] = percentComplete.Value;
        if (challengeId != null) payload["challengeId"] = challengeId;
        if (channel != null) payload["channel"] = channel;
        return Emit("achievement_unlock", payload);
    }

    /// <summary>Fetch server-ordered leaderboard standings for a ranked challenge.</summary>
    public Task<JsonElement> GetStandingsAsync(string challengeId, int limit = 20, int offset = 0)
        => RequestAsync("challenge_standings", new Dictionary<string, object>
        {
            ["challengeId"] = challengeId,
            ["limit"] = limit,
            ["offset"] = offset
        }, "challenge_standings_success");

    /// <summary>Query persisted achievement state for the connected player.</summary>
    public Task<JsonElement> GetAchievementsAsync(string? achievementId = null)
    {
        var payload = new Dictionary<string, object>();
        if (achievementId != null) payload["achievementId"] = achievementId;
        return RequestAsync("achievement_query", payload, "achievement_state");
    }

    /// <summary>Send a directed 1:1 challenge/invite to a specific player.</summary>
    public Task<JsonElement> SendChallengeInviteAsync(string toUserId, string type = "match",
        object? payload = null, int ttl = 300, string? channel = null, string? inviteId = null)
    {
        var body = new Dictionary<string, object>
        {
            ["toUserId"] = toUserId,
            ["type"] = type,
            ["ttl"] = ttl
        };
        if (payload != null) body["payload"] = payload;
        if (channel != null) body["channel"] = channel;
        if (inviteId != null) body["inviteId"] = inviteId;
        return RequestAsync("challenge_invite", body, "challenge_invite_success");
    }

    /// <summary>Accept or decline a received invite.</summary>
    public Task<JsonElement> ReplyChallengeInviteAsync(string inviteId, bool accept, string? reason = null)
    {
        var payload = new Dictionary<string, object>
        {
            ["inviteId"] = inviteId,
            ["accept"] = accept
        };
        if (reason != null) payload["reason"] = reason;
        return RequestAsync("challenge_reply", payload, "challenge_reply_success");
    }

    /// <summary>Cancel a pending invite you sent.</summary>
    public Task<JsonElement> CancelChallengeInviteAsync(string inviteId)
        => RequestAsync("challenge_invite_cancel",
            new Dictionary<string, object> { ["inviteId"] = inviteId },
            "challenge_invite_cancel_success");

    /// <summary>Pull the connected player's still-pending invites (e.g. on reconnect).</summary>
    public Task<JsonElement> GetChallengeInvitesAsync()
        => RequestAsync("challenge_invites_query", new Dictionary<string, object>(), "challenge_invites");

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
