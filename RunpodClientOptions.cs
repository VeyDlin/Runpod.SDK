namespace Runpod.SDK;


/// <summary>
/// Configuration options for RunPod client.
/// </summary>
public class RunpodClientOptions {
    /// <summary>
    /// RunPod API key. Required when using AddRunpod(), optional for AddRunpodFactory().
    /// Get your key at https://www.runpod.io/console/user/settings
    /// </summary>
    public string? apiKey { get; set; }

    /// <summary>
    /// Enable rate limiting. Default: true.
    /// Protects against hitting RunPod API rate limits.
    /// </summary>
    public bool enableRateLimiting { get; set; } = true;

    /// <summary>
    /// Custom rate limits per endpoint. Optional.
    /// If not specified, uses RunPod's documented default limits.
    /// </summary>
    public Dictionary<string, RateLimitOptions>? customRateLimits { get; set; }

    /// <summary>
    /// GraphQL API base URL. Default: https://api.runpod.io
    /// </summary>
    public string graphqlAddress { get; set; } = "https://api.runpod.io";

    /// <summary>
    /// Serverless endpoint base URL. Default: https://api.runpod.ai
    /// </summary>
    public string endpointAddress { get; set; } = "https://api.runpod.ai";
}


/// <summary>
/// Rate limit configuration for a specific endpoint.
/// </summary>
public class RateLimitOptions {
    /// <summary>
    /// Number of requests allowed per time window.
    /// </summary>
    public int requestsPerWindow { get; set; }

    /// <summary>
    /// Time window size in seconds.
    /// </summary>
    public int windowSizeSeconds { get; set; }

    /// <summary>
    /// Maximum number of concurrent requests.
    /// </summary>
    public int maxConcurrent { get; set; }
}