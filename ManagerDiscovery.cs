using System;
using System.Threading.Tasks;

namespace OddSockets;

/// <summary>
/// Simple Manager Discovery Service
/// 
/// Always connects to the main manager endpoint which handles
/// all routing and load balancing transparently.
/// </summary>
public class ManagerDiscovery
{
    private readonly string _managerUrl = "https://manager1.oddsockets.tyga.network";

    /// <summary>
    /// Gets the singleton instance of ManagerDiscovery.
    /// </summary>
    public static ManagerDiscovery Instance { get; } = new ManagerDiscovery();

    private ManagerDiscovery()
    {
    }

    /// <summary>
    /// Get the manager URL (always returns the main endpoint)
    /// </summary>
    /// <param name="apiKey">The OddSockets API key (not used, kept for compatibility)</param>
    /// <returns>The manager URL</returns>
    public Task<string> DiscoverManagerUrlAsync(string apiKey)
    {
        return Task.FromResult(_managerUrl);
    }

    /// <summary>
    /// Clear cache (no-op, kept for compatibility)
    /// </summary>
    public void ClearCache()
    {
        // No cache to clear in simplified version
    }
}
