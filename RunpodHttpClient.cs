using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK;


internal class RunpodHttpClient {
    private HttpClient baseClient { get; set; }
    private string? apiKey { get; set; }
    private string graphqlAddress { get; set; }
    private string endpointAddress { get; set; }
    private readonly RateLimiter rateLimiter = new();


    internal RunpodHttpClient(string endpointAddress, string graphqlAddress, string? apiKey = null) {
        this.endpointAddress = endpointAddress;
        this.graphqlAddress = graphqlAddress;
        this.apiKey = apiKey;

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
        if (apiKey is not null) {
            baseClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }
    }



    public async Task<T> PostAsync<T>(string endpoint, JsonObject? data = null, int timeout = 10) {
        return await rateLimiter.ExecuteAsync(endpoint, async () => {
            return await ExecuteWithRetryAsync(async () => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                var response = await baseClient.PostAsync(
                    cancellationToken: cts.Token,
                    requestUri: endpoint,
                    content: data is not null ? new StringContent(
                        data!.ToString(), 
                        Encoding.UTF8, 
                        "application/json"
                    ) : null
                );

                return await ParseResponseAsync<T>(response);
            });
        });
    }



    public async Task<T> GetAsync<T>(string endpoint, int timeout = 10) {
        return await rateLimiter.ExecuteAsync(endpoint, async () => {
            return await ExecuteWithRetryAsync(async () => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(timeout));

                var response = await baseClient.GetAsync(
                    cancellationToken: cts.Token,
                    requestUri: endpoint
                );

                return await ParseResponseAsync<T>(response);
            });
        });
    }



    public async Task<T> RunGraphQLAsync<T>(string query, int timeout = 100) {
        var json = await PostAsync<JsonObject>(
            endpoint: $"{graphqlAddress}/graphql?api_key={apiKey}",
            data: new() { 
                ["query"] = query 
            },
            timeout: timeout
        );

        if (json!.ContainsKey("errors")) {
            var errorMessage = json["errors"]?[0]?["message"]?.ToString();
            throw new QueryException(errorMessage ?? "Unknown error occurred", query.ToString());
        }

        return json.Deserialize<T>()!;
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
}