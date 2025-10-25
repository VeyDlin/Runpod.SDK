using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK.Endpoints;


/// <summary>
/// Represents a RunPod serverless endpoint for running jobs.
/// Provides methods for synchronous/asynchronous execution, health checks, and queue management.
/// </summary>
public class Endpoint {
    private string endpointId;
    private RunpodHttpClient client;
    private string apiKey;


    internal Endpoint(RunpodHttpClient client, string endpointId, string apiKey) {
        this.client = client;
        this.endpointId = endpointId;
        this.apiKey = apiKey;
    }



    /// <summary>
    /// Runs a task synchronously and waits for completion.
    /// Best for real-time tasks like image generation or LLM inference.
    /// </summary>
    /// <typeparam name="T">Expected result type.</typeparam>
    /// <param name="requestInput">Input data for the endpoint.</param>
    /// <param name="pollingOptions">Polling configuration. Default: 1 second polling, 24h timeout.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>Task result of type T.</returns>
    /// <exception cref="JobErrorException">Thrown when job fails or is cancelled.</exception>
    /// <exception cref="TimeoutException">Thrown when job exceeds timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    public async Task<T> RunSync<T>(
        object requestInput,
        PollingOptions? pollingOptions = null,
        CancellationToken cancellationToken = default
    ) {
        pollingOptions ??= new PollingOptions { timeout = 86400 };  // Default 24h for sync

        var input = GetRequestObject(requestInput);
        var request = await client.PostAsync<JsonNode>(
            $"v2/{endpointId}/runsync",
            input,
            pollingOptions.timeout,
            apiKey
        );

        var status = request["status"]!.ToString();
        if (Job.IsFinal(status)) {
            if (Job.IsError(status)) {
                throw new JobErrorException(status, request["error"]?.ToString());
            }
            return request.Deserialize<T>()!;
        }

        var job = new Job(client, endpointId, request["id"]!.ToString(), apiKey);
        return await job.Output<T>(pollingOptions, cancellationToken);
    }


    /// <summary>
    /// Runs a task synchronously with simple polling parameters.
    /// Backward compatibility method.
    /// </summary>
    /// <typeparam name="T">Expected result type.</typeparam>
    /// <param name="requestInput">Input data for the endpoint.</param>
    /// <param name="updateDelay">Delay between status checks in milliseconds. Default: 1000ms.</param>
    /// <param name="timeout">Timeout in seconds. Default: 86400s (24 hours).</param>
    /// <returns>Task result of type T.</returns>
    public async Task<T> RunSync<T>(object requestInput, int updateDelay = 1000, int timeout = 86400) {
        return await RunSync<T>(requestInput, new PollingOptions {
            updateDelay = updateDelay,
            timeout = timeout
        });
    }



    /// <summary>
    /// Submits a job for asynchronous execution.
    /// Returns immediately with a Job object for tracking status and retrieving results.
    /// </summary>
    /// <param name="requestInput">Input data for the endpoint.</param>
    /// <returns>Job instance for tracking and result retrieval.</returns>
    public async Task<Job> Run(object requestInput) {
        var input = GetRequestObject(requestInput);
        var request = await client.PostAsync<JsonNode>($"v2/{endpointId}/run", input, apiKey: apiKey);
        return new Job(client, endpointId, request["id"]!.ToString(), apiKey);
    }



    /// <summary>
    /// Checks endpoint health status including worker count and queue depth.
    /// </summary>
    /// <returns>Health status information.</returns>
    public async Task<object?> Health() {
        return await client.GetAsync<object>($"v2/{endpointId}/health", apiKey: apiKey);
    }



    /// <summary>
    /// Purges all data and jobs from the endpoint.
    /// </summary>
    /// <returns>Purge operation result.</returns>
    public async Task<object?> Purge() {
        return await client.PostAsync<object>($"v2/{endpointId}/purge", null, apiKey: apiKey);
    }



    /// <summary>
    /// Purges pending jobs from the endpoint queue.
    /// </summary>
    /// <returns>Purge queue operation result.</returns>
    public async Task<object?> PurgeQueue() {
        return await client.PostAsync<object>($"v2/{endpointId}/purge-queue", null, apiKey: apiKey);
    }



    private JsonObject GetRequestObject(object requestInput) {
        var jsonString = JsonSerializer.Serialize(requestInput);
        var input = JsonNode.Parse(jsonString)!.AsObject();
        if (!input.ContainsKey("input")) {
            input = new JsonObject {
                ["input"] = JsonSerializer.SerializeToNode(requestInput)
            };
        }
        return input;
    }
}