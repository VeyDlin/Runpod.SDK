using Runpod.SDK.Infrastructure;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK.Client;


internal class RunpodHttpClient : IDisposable {
    private HttpClient baseClient { get; set; }
    private string graphqlAddress { get; set; }
    private readonly RateLimiter rateLimiter = new();
    private bool disposed = false;
    private readonly bool ownsHttpClient;


    /// <summary>
    /// Initializes a new instance for standalone usage.
    /// Creates and owns the HttpClient.
    /// </summary>
    internal RunpodHttpClient(string endpointAddress, string graphqlAddress) {
        this.graphqlAddress = graphqlAddress;
        this.ownsHttpClient = true;

        var handler = new SocketsHttpHandler {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true
        };

        baseClient = new(handler) {
            Timeout = TimeSpan.FromMinutes(10),
            BaseAddress = new Uri(endpointAddress)
        };

        baseClient.DefaultRequestHeaders.Add("User-Agent", string.Join(" ", new List<string> {
            $"({RuntimeInformation.OSDescription}; {RuntimeInformation.ProcessArchitecture})",
            $"Language/.NET {Environment.Version}"
        }));
    }


    /// <summary>
    /// Initializes a new instance using an externally provided HttpClient.
    /// Does not own or dispose the HttpClient.
    /// </summary>
    internal RunpodHttpClient(HttpClient httpClient, string graphqlAddress) {
        this.baseClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.graphqlAddress = graphqlAddress;
        this.ownsHttpClient = false;
    }



    public async Task<T> PostAsync<T>(string endpoint, JsonObject? data = null, int timeout = 10, string? apiKey = null) {
        return await rateLimiter.ExecuteAsync(endpoint, async () => {
            return await ExecuteWithRetryAsync(async () => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                var request = new HttpRequestMessage(HttpMethod.Post, endpoint);

                if (apiKey is not null) {
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                }

                if (data is not null) {
                    request.Content = new StringContent(
                        data.ToString(),
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                var response = await baseClient.SendAsync(request, cts.Token);
                return await ParseResponseAsync<T>(response);
            });
        });
    }



    public async Task<T> GetAsync<T>(string endpoint, int timeout = 10, string? apiKey = null) {
        return await rateLimiter.ExecuteAsync(endpoint, async () => {
            return await ExecuteWithRetryAsync(async () => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                var request = new HttpRequestMessage(HttpMethod.Get, endpoint);

                if (apiKey is not null) {
                    request.Headers.Add("Authorization", $"Bearer {apiKey}");
                }

                var response = await baseClient.SendAsync(request, cts.Token);
                return await ParseResponseAsync<T>(response);
            });
        });
    }



    /// <summary>
    /// Creates a GraphQL client for executing queries and mutations.
    /// </summary>
    /// <param name="apiKey">RunPod API key for authentication.</param>
    /// <returns>GraphQL client instance.</returns>
    internal GraphQLClient CreateGraphQLClient(string apiKey) {
        return new GraphQLClient(this, graphqlAddress, apiKey);
    }



    private async Task<T> ParseResponseAsync<T>(HttpResponseMessage response) {
        if (response.StatusCode == HttpStatusCode.Unauthorized) {
            throw new HttpRequestException("401 Unauthorized | Make sure Runpod API key is set and valid.");
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var jsonNode = JsonNode.Parse(content);
        return jsonNode!.Deserialize<T>()!;
    }



    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, int maxRetries = 4) {
        var retryCount = 0;
        while (true) {
            try {
                return await operation();
            } catch (Exception ex) when (retryCount < maxRetries && ShouldRetry(ex)) {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                await Task.Delay(delay);
            }
        }
    }



    private bool ShouldRetry(Exception ex) {
        return ex is HttpRequestException httpEx &&
            (
                httpEx.InnerException is IOException ||
                (
                    httpEx.InnerException is System.Net.Sockets.SocketException sockEx &&
                    IsTransientSocketError(sockEx)
                ) ||
                httpEx.Message.Contains("SSL connection could not be established")
            );
    }



    private bool IsTransientSocketError(System.Net.Sockets.SocketException sockEx) {
        return sockEx.SocketErrorCode switch {
            System.Net.Sockets.SocketError.NetworkUnreachable => true,
            System.Net.Sockets.SocketError.TimedOut => true,
            System.Net.Sockets.SocketError.ConnectionRefused => true,
            System.Net.Sockets.SocketError.ConnectionReset => true,
            System.Net.Sockets.SocketError.ConnectionAborted => true,
            System.Net.Sockets.SocketError.HostUnreachable => true,
            _ => false
        };
    }



    public void Dispose() {
        if (!disposed) {
            if (ownsHttpClient) {
                baseClient?.Dispose();
            }
            disposed = true;
        }
    }
}