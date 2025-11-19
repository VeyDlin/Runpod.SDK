namespace Runpod.SDK.Client;


/// <summary>
/// Factory for creating RunPod client instances with different API keys.
/// Supports both ASP.NET Core DI with IHttpClientFactory and standalone usage.
/// All created clients share the same HttpClient pool for optimal performance and resource usage.
/// </summary>
public class RunpodClientFactory : IRunpodClientFactory, IDisposable {
    private const string HttpClientName = "Runpod";

    private readonly IHttpClientFactory? httpClientFactory;
    private readonly RunpodHttpClient? sharedHttpClient;
    private readonly RunpodClientOptions options;
    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance for use with ASP.NET Core dependency injection.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory from DI.</param>
    /// <param name="options">Configuration options.</param>
    public RunpodClientFactory(
        IHttpClientFactory httpClientFactory,
        RunpodClientOptions options
    ) {
        this.httpClientFactory = httpClientFactory
            ?? throw new ArgumentNullException(nameof(httpClientFactory));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Initializes a new instance for standalone usage (console applications).
    /// Remember to dispose the factory when done.
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
    /// Gets the HTTP client name used for DI registration.
    /// </summary>
    internal static string GetHttpClientName() => HttpClientName;

    /// <summary>
    /// Creates a RunPod client instance with the specified API key.
    /// The client shares the HttpClient pool with all other clients created by this factory.
    /// </summary>
    /// <param name="apiKey">RunPod API key for this client instance.</param>
    /// <returns>A new RunpodClient instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the factory has been disposed.</exception>
    public IRunpodClient CreateClient(string apiKey) {
        if (disposed) {
            throw new ObjectDisposedException(nameof(RunpodClientFactory));
        }

        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
        }

        RunpodHttpClient httpClient;

        if (httpClientFactory != null) {
            // Use IHttpClientFactory for proper connection management
            var client = httpClientFactory.CreateClient(HttpClientName);
            httpClient = new RunpodHttpClient(client, options.graphqlAddress);
        } else if (sharedHttpClient != null) {
            // Use shared HttpClient for standalone mode
            httpClient = sharedHttpClient;
        } else {
            throw new InvalidOperationException("Factory not properly initialized.");
        }

        return new RunpodClient(apiKey, httpClient);
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
