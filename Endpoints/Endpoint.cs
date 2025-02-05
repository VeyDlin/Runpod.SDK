using Newtonsoft.Json.Linq;

namespace Runpod.SDK.Endpoints;


public class Endpoint {
    private string endpointId;
    private RunpodHttpClient client;


    internal Endpoint(RunpodHttpClient client, string endpointId) {
        this.client = client;
        this.endpointId = endpointId;
    }



    public async Task<T> RunSync<T>(object requestInput, int updateDelay = 500, int timeout = 86400) {
        var input = GetRequestObject(requestInput);
        var request = await client.PostAsync<JToken>($"v2/{endpointId}/runsync", input, timeout);
        var job = new Job(client, endpointId, request["id"]!.ToString());
        return await job.Output<T>(updateDelay, timeout);
    }



    public async Task<Job> Run(object requestInput) {
        var input = GetRequestObject(requestInput);
        var request = await client.PostAsync<JToken>($"v2/{endpointId}/run", input);
        return new Job(client, endpointId, request["id"]!.ToString());
    }



    public async Task<object?> Health() {
        return await client.GetAsync<object>($"v2/{endpointId}/health");
    }



    public async Task<object?> Purge() {
        return await client.PostAsync<object>($"v2/{endpointId}/purge", null);
    }



    public async Task<object?> PurgeQueue() {
        return await client.PostAsync<object>($"v2/{endpointId}/purge-queue", null);
    }



    private JObject GetRequestObject(object requestInput) {
        var input = JObject.FromObject(requestInput);
        if (!input.ContainsKey("input")) {
            input = new JObject {
                ["input"] = JObject.FromObject(requestInput)
            };
        }
        return input;
    }
}