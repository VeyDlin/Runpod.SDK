using Runpod.SDK.API;
using Runpod.SDK.Endpoints;

namespace Runpod.SDK.Client;


/// <summary>
/// RunPod API client for managing pods and serverless endpoints.
/// </summary>
public class RunpodClient : IRunpodClient {
    private RunpodHttpClient client { get; set; }
    private string apiKey { get; set; }
    private bool ownsHttpClient { get; set; }

    /// <summary>
    /// GraphQL commands for pod and GPU management.
    /// </summary>
    public Commands cmd { get; private set; }

    /// <summary>
    /// Creates an endpoint client for serverless operations.
    /// </summary>
    /// <param name="id">The endpoint ID.</param>
    /// <returns>An endpoint client instance.</returns>
    public Endpoint Endpoint(string id) => new(client, id, apiKey);


    /// <summary>
    /// Initializes a new instance of RunpodClient with an API key.
    /// Creates its own HttpClient instance.
    /// </summary>
    /// <param name="apiKey">RunPod API key. Get yours at https://www.runpod.io/console/user/settings</param>
    /// <exception cref="ArgumentNullException">Thrown when apiKey is null or empty.</exception>
    public RunpodClient(string apiKey) {
        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new ArgumentNullException(
                nameof(apiKey),
                "RunPod API key is required. Get yours at https://www.runpod.io/console/user/settings"
            );
        }

        var defaultOptions = new RunpodClientOptions();
        this.apiKey = apiKey;
        client = new(defaultOptions.endpointAddress, defaultOptions.graphqlAddress);
        cmd = new(client, apiKey);
        ownsHttpClient = true;
    }


    /// <summary>
    /// Internal constructor for factory pattern.
    /// Uses a shared HttpClient instance.
    /// </summary>
    /// <param name="apiKey">RunPod API key.</param>
    /// <param name="sharedClient">Shared HttpClient wrapper.</param>
    internal RunpodClient(string apiKey, RunpodHttpClient sharedClient) {
        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
        }

        this.apiKey = apiKey;
        client = sharedClient;
        cmd = new(client, apiKey);
        ownsHttpClient = false;  // Don't dispose shared client
    }


    /// <summary>
    /// Disposes the HTTP client if this instance owns it.
    /// </summary>
    public void Dispose() {
        if (ownsHttpClient) {
            client?.Dispose();
        }
    }
}