using System;
using System.Threading.Tasks;

namespace OddSockets;

/// <summary>
/// Manager Discovery Service
///
/// Resolves the manager endpoint that a client should talk to. The manager
/// handles all worker routing and load balancing transparently.
///
/// Resolution order is: explicit configuration, then the ODDSOCKETS_MANAGER_URL
/// environment variable, then the public default endpoint. The default is only
/// ever used when nothing was configured at all - a configured manager that is
/// unreachable must surface that failure, because silently redirecting a
/// self-hosted or staging client to production makes a misconfigured deployment
/// look healthy.
/// </summary>
public class ManagerDiscovery
{
    /// <summary>
    /// The public manager endpoint, used only when no manager URL was configured.
    /// </summary>
    public const string DefaultManagerUrl = "https://connect.oddsockets.tyga.network";

    /// <summary>
    /// Environment variable consulted when no manager URL was configured explicitly.
    /// </summary>
    public const string ManagerUrlEnvironmentVariable = "ODDSOCKETS_MANAGER_URL";

    /// <summary>
    /// Gets the singleton instance of ManagerDiscovery. It holds no per-client state:
    /// the manager URL is always supplied by the caller so that one client's
    /// configuration cannot leak into another's.
    /// </summary>
    public static ManagerDiscovery Instance { get; } = new ManagerDiscovery();

    private ManagerDiscovery()
    {
    }

    /// <summary>
    /// Resolves the manager URL for a client.
    /// </summary>
    /// <param name="apiKey">The OddSockets API key.</param>
    /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be null.</param>
    /// <returns>The resolved manager URL, without any trailing slash.</returns>
    /// <exception cref="ArgumentException">Thrown when the resolved URL is not an absolute http(s) URL.</exception>
    public Task<string> DiscoverManagerUrlAsync(string apiKey, string? configuredManagerUrl)
    {
        return Task.FromResult(ResolveManagerUrl(configuredManagerUrl));
    }

    /// <summary>
    /// Resolves and validates a manager URL.
    /// </summary>
    /// <param name="configuredManagerUrl">The manager URL from the client configuration, may be null.</param>
    /// <returns>The resolved manager URL, without any trailing slash.</returns>
    /// <exception cref="ArgumentException">Thrown when the resolved URL is not an absolute http(s) URL.</exception>
    public static string ResolveManagerUrl(string? configuredManagerUrl)
    {
        var candidate = configuredManagerUrl;

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = Environment.GetEnvironmentVariable(ManagerUrlEnvironmentVariable);
        }

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = DefaultManagerUrl;
        }

        return ValidateManagerUrl(candidate!);
    }

    /// <summary>
    /// Clear cache (no-op, kept for compatibility)
    /// </summary>
    public void ClearCache()
    {
        // No cache to clear in simplified version
    }

    private static string ValidateManagerUrl(string managerUrl)
    {
        var candidate = managerUrl.Trim();

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrEmpty(uri.Host))
        {
            throw new ArgumentException($"Invalid managerUrl: {managerUrl}");
        }

        return candidate.TrimEnd('/');
    }
}
