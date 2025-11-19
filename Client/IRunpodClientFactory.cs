namespace Runpod.SDK.Client;


/// <summary>
/// Factory for creating RunPod client instances with different API keys.
/// Use this for multi-tenant scenarios where API keys are retrieved dynamically.
/// </summary>
public interface IRunpodClientFactory {
    /// <summary>
    /// Creates a RunPod client instance with the specified API key.
    /// All clients share the same HttpClient pool for optimal performance.
    /// </summary>
    /// <param name="apiKey">RunPod API key for this client instance.</param>
    /// <returns>A new RunpodClient instance.</returns>
    IRunpodClient CreateClient(string apiKey);
}
