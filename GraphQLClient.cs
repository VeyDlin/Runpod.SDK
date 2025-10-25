using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK;


/// <summary>
/// Client for executing GraphQL queries and mutations against RunPod API.
/// </summary>
internal class GraphQLClient {
    private readonly RunpodHttpClient httpClient;
    private readonly string graphqlEndpoint;
    private readonly string apiKey;


    /// <summary>
    /// Initializes a new instance of the GraphQLClient.
    /// </summary>
    /// <param name="httpClient">HTTP client wrapper for making requests.</param>
    /// <param name="graphqlEndpoint">Base URL for GraphQL endpoint.</param>
    /// <param name="apiKey">RunPod API key for authentication.</param>
    internal GraphQLClient(RunpodHttpClient httpClient, string graphqlEndpoint, string apiKey) {
        this.httpClient = httpClient;
        this.graphqlEndpoint = graphqlEndpoint;
        this.apiKey = apiKey;
    }


    /// <summary>
    /// Executes a GraphQL query or mutation.
    /// </summary>
    /// <typeparam name="T">Expected response type.</typeparam>
    /// <param name="query">GraphQL query or mutation string.</param>
    /// <param name="timeout">Timeout in seconds. Default: 100 seconds.</param>
    /// <returns>Deserialized response of type T.</returns>
    /// <exception cref="QueryException">Thrown when GraphQL returns errors.</exception>
    internal async Task<T> ExecuteAsync<T>(string query, int timeout = 100) {
        var json = await httpClient.PostAsync<JsonObject>(
            endpoint: $"{graphqlEndpoint}/graphql",
            data: new() {
                ["query"] = query
            },
            timeout: timeout,
            apiKey: apiKey
        );

        if (json!.ContainsKey("errors")) {
            var errorMessage = json["errors"]?[0]?["message"]?.ToString();
            throw new QueryException(errorMessage ?? "Unknown GraphQL error occurred", query);
        }

        return JsonSerializer.Deserialize<T>(json)!;
    }


    /// <summary>
    /// Executes a GraphQL query and extracts data from a specific path.
    /// </summary>
    /// <typeparam name="T">Expected response type.</typeparam>
    /// <param name="query">GraphQL query or mutation string.</param>
    /// <param name="dataPath">Path to extract from response (e.g., "data.myself.pods").</param>
    /// <param name="timeout">Timeout in seconds. Default: 100 seconds.</param>
    /// <returns>Extracted data of type T.</returns>
    /// <exception cref="QueryException">Thrown when GraphQL returns errors.</exception>
    internal async Task<JsonNode> ExecuteAndExtractAsync(string query, string dataPath, int timeout = 100) {
        var response = await ExecuteAsync<JsonNode>(query, timeout);

        var current = response;
        foreach (var segment in dataPath.Split('.')) {
            current = current?[segment];
            if (current is null) {
                throw new QueryException($"Failed to extract path '{dataPath}' from GraphQL response", query);
            }
        }

        return current;
    }
}
