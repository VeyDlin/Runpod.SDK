namespace Runpod.SDK;

public class RunpodClientOptions {
    public Dictionary<string, RateLimitOptions>? customRateLimits { get; set; }
    public bool enableRateLimiting { get; set; } = true;
}

public class RateLimitOptions {
    public int requestsPerWindow { get; set; }
    public int windowSizeSeconds { get; set; }
    public int maxConcurrent { get; set; }
}