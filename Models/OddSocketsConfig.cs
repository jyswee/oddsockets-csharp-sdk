using System.ComponentModel.DataAnnotations;

namespace OddSockets.Models;

/// <summary>
/// Configuration options for the OddSockets client.
/// </summary>
public class OddSocketsConfig
{
    /// <summary>
    /// Gets or sets the OddSockets API key (required).
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the manager URL (optional). When not set explicitly it falls back to the
    /// ODDSOCKETS_MANAGER_URL environment variable and then to the public default endpoint.
    /// A URL set here is always used verbatim; the client never substitutes the default
    /// endpoint when the configured manager is unreachable.
    /// </summary>
    public string ManagerUrl { get; set; } = ManagerDiscovery.ResolveManagerUrl(null);

    /// <summary>
    /// Gets or sets the user identifier (optional, auto-generated if not provided).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or sets whether the client should auto-connect on creation (default: true).
    /// </summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of reconnection attempts (default: 5).
    /// </summary>
    public int ReconnectAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the heartbeat interval in seconds (default: 30).
    /// </summary>
    public int HeartbeatInterval { get; set; } = 30;

    /// <summary>
    /// Gets or sets the request timeout in seconds (default: 10).
    /// </summary>
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new ArgumentException("API key is required", nameof(ApiKey));

        if (!ApiKey.StartsWith("ak_"))
            throw new ArgumentException("Invalid API key format", nameof(ApiKey));

        if (string.IsNullOrWhiteSpace(ManagerUrl))
            throw new ArgumentException("Manager URL is required", nameof(ManagerUrl));

        ManagerUrl = ManagerDiscovery.ResolveManagerUrl(ManagerUrl);

        if (ReconnectAttempts < 0)
            throw new ArgumentException("Reconnect attempts must be non-negative", nameof(ReconnectAttempts));

        if (HeartbeatInterval <= 0)
            throw new ArgumentException("Heartbeat interval must be positive", nameof(HeartbeatInterval));

        if (Timeout <= 0)
            throw new ArgumentException("Timeout must be positive", nameof(Timeout));
    }
}

/// <summary>
/// Builder class for creating OddSocketsConfig instances.
/// </summary>
public class OddSocketsConfigBuilder
{
    private readonly OddSocketsConfig _config = new();

    /// <summary>
    /// Sets the API key.
    /// </summary>
    /// <param name="apiKey">The API key.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithApiKey(string apiKey)
    {
        _config.ApiKey = apiKey;
        return this;
    }

    /// <summary>
    /// Sets the manager URL. When left unset the ODDSOCKETS_MANAGER_URL environment
    /// variable is used, falling back to the public default endpoint.
    /// </summary>
    /// <param name="managerUrl">The manager URL, must be an absolute http(s) URL.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithManagerUrl(string managerUrl)
    {
        _config.ManagerUrl = managerUrl;
        return this;
    }

    /// <summary>
    /// Sets the user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithUserId(string userId)
    {
        _config.UserId = userId;
        return this;
    }

    /// <summary>
    /// Sets whether to auto-connect.
    /// </summary>
    /// <param name="autoConnect">Whether to auto-connect.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithAutoConnect(bool autoConnect)
    {
        _config.AutoConnect = autoConnect;
        return this;
    }

    /// <summary>
    /// Sets the reconnect attempts.
    /// </summary>
    /// <param name="attempts">The number of reconnect attempts.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithReconnectAttempts(int attempts)
    {
        _config.ReconnectAttempts = attempts;
        return this;
    }

    /// <summary>
    /// Sets the heartbeat interval.
    /// </summary>
    /// <param name="interval">The heartbeat interval in seconds.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithHeartbeatInterval(int interval)
    {
        _config.HeartbeatInterval = interval;
        return this;
    }

    /// <summary>
    /// Sets the timeout.
    /// </summary>
    /// <param name="timeout">The timeout in seconds.</param>
    /// <returns>The builder instance.</returns>
    public OddSocketsConfigBuilder WithTimeout(int timeout)
    {
        _config.Timeout = timeout;
        return this;
    }

    /// <summary>
    /// Builds the configuration.
    /// </summary>
    /// <returns>The configured OddSocketsConfig instance.</returns>
    /// <exception cref="ArgumentException">Thrown when configuration is invalid.</exception>
    public OddSocketsConfig Build()
    {
        _config.Validate();
        return _config;
    }
}
