using Newtonsoft.Json.Linq;
using Runpod;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Runpod.SDK;


internal class RunpodHttpClient {
    private HttpClient baseClient { get; set; }
    private string? apiKey { get; set; }
    private string graphqlAddress { get; set; }
    private string endpointAddress { get; set; }


    internal RunpodHttpClient(string endpointAddress, string graphqlAddress, string? apiKey = null) {
        this.endpointAddress = endpointAddress;
        this.graphqlAddress = graphqlAddress;
        this.apiKey = apiKey;

        baseClient = new() {
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



    public async Task<T> PostAsync<T>(string endpoint, JObject? data = null, int timeout = 10) {
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
    }



    public async Task<T> GetAsync<T>(string endpoint, int timeout = 10) {
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(timeout));

        var response = await baseClient.GetAsync(
            cancellationToken: cts.Token,
            requestUri: endpoint
        );

        return await ParseResponseAsync<T>(response);
    }



    public async Task<T> RunGraphQLAsync<T>(string query, int timeout = 100) {
        var json = await PostAsync<JObject>(
            endpoint: $"{graphqlAddress}/graphql?api_key={apiKey}",
            data: new() { 
                { "query", query } 
            },
            timeout: timeout
        );

        if (json!.ContainsKey("errors")) {
            var errorMessage = json["errors"]?[0]?["message"]?.ToString();
            throw new QueryException(errorMessage ?? "Unknown error occurred", query.ToString());
        }

        return json.ToObject<T>()!;
    }



    private async Task<T> ParseResponseAsync<T>(HttpResponseMessage response) {
        if (response.StatusCode == HttpStatusCode.Unauthorized) {
            throw new HttpRequestException("401 Unauthorized | Make sure Runpod API key is set and valid.");
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        return JObject.Parse(content).ToObject<T>()!;
    }
}