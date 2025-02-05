using Runpod.SDK.API;
using Runpod.SDK.Endpoints;

namespace Runpod.SDK;


public class RunpodClient {
    private static string graphqlAddress { get; } = $"https://api.runpod.io";
    private static string endpointAddress { get; } = $"https://api.runpod.ai";
    private RunpodHttpClient client { get; set; }

    public Commands cmd { get; private set; }
    public Endpoint Endpoint(string id) => new(client, id);


    public RunpodClient(string? apiKey = null) {
        client = new(endpointAddress, graphqlAddress, apiKey);
        cmd = new(client);
    }
}