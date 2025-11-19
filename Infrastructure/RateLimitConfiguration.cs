namespace Runpod.SDK.Infrastructure;

internal class RateLimitConfiguration {
    public int requestsPerWindow { get; set; }
    public int windowSizeSeconds { get; set; }
    public int maxConcurrent { get; set; }

    public static readonly Dictionary<string, RateLimitConfiguration> EndpointLimits = new() {
        // /run endpoint: 1000 requests per 10 seconds, max 200 concurrent
        ["run"] = new() { requestsPerWindow = 1000, windowSizeSeconds = 10, maxConcurrent = 200 },

        // /runsync endpoint: 2000 requests per 10 seconds, max 400 concurrent
        ["runsync"] = new() { requestsPerWindow = 2000, windowSizeSeconds = 10, maxConcurrent = 400 },

        // /status, /status-sync, /stream endpoints: 2000 requests per 10 seconds, max 400 concurrent
        ["status"] = new() { requestsPerWindow = 2000, windowSizeSeconds = 10, maxConcurrent = 400 },
        ["stream"] = new() { requestsPerWindow = 2000, windowSizeSeconds = 10, maxConcurrent = 400 },

        // /cancel endpoint: 100 requests per 10 seconds, max 20 concurrent
        ["cancel"] = new() { requestsPerWindow = 100, windowSizeSeconds = 10, maxConcurrent = 20 },

        // /purge-queue endpoint: 2 requests per 10 seconds
        ["purge-queue"] = new() { requestsPerWindow = 2, windowSizeSeconds = 10, maxConcurrent = 1 },

        // /openai/* endpoints: 2000 requests per 10 seconds, max 400 concurrent
        ["openai"] = new() { requestsPerWindow = 2000, windowSizeSeconds = 10, maxConcurrent = 400 },

        // /requests endpoint: 10 requests per 10 seconds, max 2 concurrent
        ["requests"] = new() { requestsPerWindow = 10, windowSizeSeconds = 10, maxConcurrent = 2 },

        // Default for other endpoints
        ["default"] = new() { requestsPerWindow = 1000, windowSizeSeconds = 10, maxConcurrent = 100 }
    };

    public static RateLimitConfiguration GetLimitForEndpoint(string endpoint) {
        endpoint = endpoint.ToLower();

        // Check more specific endpoints first
        if (endpoint.Contains("/runsync")) {
            return EndpointLimits["runsync"];
        }
        if (endpoint.Contains("/run")) {
            return EndpointLimits["run"];
        }
        if (endpoint.Contains("/purge-queue")) {
            return EndpointLimits["purge-queue"];
        }

        // Then check other endpoints
        foreach (var kvp in EndpointLimits) {
            if (kvp.Key != "run" && kvp.Key != "runsync" && kvp.Key != "purge-queue" && kvp.Key != "default") {
                if (endpoint.Contains(kvp.Key)) {
                    return kvp.Value;
                }
            }
        }

        return EndpointLimits["default"];
    }
}