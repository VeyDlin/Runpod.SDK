namespace Runpod.SDK;


/// <summary>
/// Factory for creating RunPod client instances with different API keys.
/// All created clients share the same HttpClient pool for optimal performance and resource usage.
/// </summary>
public class RunpodClientFactory : IRunpodClientFactory, IDisposable {
    private readonly RunpodHttpClient sharedHttpClient;
    private readonly RunpodClientOptions options;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the RunpodClientFactory.
    /// </summary>
    /// <param name="options">Configuration options. If null, uses defaults.</param>
    public RunpodClientFactory(RunpodClientOptions? options = null) {
        this.options = options ?? new RunpodClientOptions();

        // One shared HttpClient for all client instances = shared connection pool
        sharedHttpClient = new RunpodHttpClient(
            this.options.endpointAddress,
            this.options.graphqlAddress
        );
    }

    /// <summary>
    /// Creates a RunPod client instance with the specified API key.
    /// The client shares the HttpClient pool with all other clients created by this factory.
    /// </summary>
    /// <param name="apiKey">RunPod API key for this client instance.</param>
    /// <returns>A new RunpodClient instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public IRunpodClient CreateClient(string apiKey) {
        if (disposed) {
            throw new ObjectDisposedException(nameof(RunpodClientFactory));
        }

        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
        }

        // Create client with shared HttpClient - no need to dispose individual clients
        return new RunpodClient(apiKey, sharedHttpClient);
    }

    /// <summary>
    /// Disposes the shared HttpClient pool.
    /// </summary>
    public void Dispose() {
        if (!disposed) {
            sharedHttpClient?.Dispose();
            disposed = true;
        }
    }
}
