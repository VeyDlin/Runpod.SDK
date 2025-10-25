namespace Runpod.SDK;


/// <summary>
/// Options for job status polling behavior.
/// </summary>
public class PollingOptions {
    /// <summary>
    /// Delay between status checks in milliseconds. Default: 1000ms (1 second).
    /// For real-time applications (image generation, LLM), keep this low for fast user feedback.
    /// </summary>
    public int updateDelay { get; set; } = 1000;

    /// <summary>
    /// Enable progressive backoff for long-running tasks. Default: false.
    /// When enabled, delay increases: 1s -> 2s -> 4s -> up to MaxDelay.
    /// Use this for batch jobs that run for hours/days to reduce API calls.
    /// </summary>
    public bool useProgressiveBackoff { get; set; } = false;

    /// <summary>
    /// Maximum delay between checks when progressive backoff is enabled. Default: 60000ms (1 minute).
    /// </summary>
    public int maxDelay { get; set; } = 60000;

    /// <summary>
    /// Timeout in seconds. 0 = no timeout (wait indefinitely). Default: 0.
    /// </summary>
    public int timeout { get; set; } = 0;
}
