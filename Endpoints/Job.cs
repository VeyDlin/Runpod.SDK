using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK.Endpoints;


/// <summary>
/// Represents an asynchronous job submitted to a RunPod serverless endpoint.
/// Provides methods for polling status, retrieving results, streaming output, and cancellation.
/// </summary>
public class Job {
    private string endpointId;
    private string jobId;
    private RunpodHttpClient client;
    private string apiKey;

    private string? jobStatus;
    private JsonNode? jobOutput;
    private object syncLock = new();


    internal Job(RunpodHttpClient client, string endpointId, string jobId, string apiKey) {
        this.client = client;
        this.endpointId = endpointId;
        this.jobId = jobId;
        this.apiKey = apiKey;
    }



    /// <summary>
    /// Retrieves the current status of the job.
    /// Returns cached status if job has reached a final state (completed/failed/cancelled).
    /// </summary>
    /// <returns>Job status string (e.g., "IN_QUEUE", "IN_PROGRESS", "COMPLETED", "FAILED").</returns>
    /// <exception cref="JobErrorException">Thrown when job status is FAILED, TIMED_OUT, or CANCELLED.</exception>
    public async Task<string> Status() {
        lock (syncLock) {
            if (jobStatus is not null) {
                return jobStatus;
            }
        }

        var response = await client.GetAsync<JsonNode>($"v2/{endpointId}/status/{jobId}", apiKey: apiKey);
        var status = response["status"]!.ToString();

        if (IsFinal(status)) {
            lock (syncLock) {
                jobStatus = response["status"]!.ToString();
                if (IsError(jobStatus)) {
                    throw new JobErrorException(jobStatus, response["error"]?.ToString());
                }
                jobOutput = response["output"]!;
            }
        }

        return status;
    }



    /// <summary>
    /// Polls job status until completion and returns the result.
    /// Returns cached output if job has already completed.
    /// </summary>
    /// <typeparam name="T">Expected result type.</typeparam>
    /// <param name="options">Polling configuration. Default: 1 second polling, no timeout, no progressive backoff.</param>
    /// <param name="cancellationToken">Cancellation token to abort the operation.</param>
    /// <returns>Job result of type T.</returns>
    /// <exception cref="JobErrorException">Thrown when job fails or is cancelled.</exception>
    /// <exception cref="TimeoutException">Thrown when job exceeds timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled.</exception>
    public async Task<T> Output<T>(
        PollingOptions? options = null,
        CancellationToken cancellationToken = default
    ) {
        options ??= new PollingOptions();

        lock (syncLock) {
            if (jobOutput is not null) {
                return jobOutput!.Deserialize<T>()!;
            }
        }

        var waitForCompletion = async () => {
            var currentDelay = options.updateDelay;

            try {
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();

                    var response = await client.GetAsync<JsonNode>(
                        $"v2/{endpointId}/status/{jobId}",
                        apiKey: apiKey
                    );

                    if (!IsFinal(response["status"]!.ToString())) {
                        await Task.Delay(currentDelay, cancellationToken);

                        // Progressive backoff only if enabled
                        if (options.useProgressiveBackoff) {
                            currentDelay = Math.Min(currentDelay * 2, options.maxDelay);
                        }

                        continue;
                    }

                    lock (syncLock) {
                        jobStatus = response["status"]!.ToString();
                        if (IsError(jobStatus)) {
                            throw new JobErrorException(jobStatus, response["error"]?.ToString());
                        }
                        jobOutput = response["output"]!;
                    }
                    break;
                }
            } catch (OperationCanceledException) {
                try {
                    await Cancel();
                } catch {
                    // Best effort - ignore if cancel API call fails
                }
                throw;
            }
        };

        var task = waitForCompletion();

        if (options.timeout > 0) {
            var timeoutTask = Task.Delay(
                TimeSpan.FromSeconds(options.timeout),
                cancellationToken
            );

            if (await Task.WhenAny(task, timeoutTask) == timeoutTask) {
                // Cancel the job on RunPod side (works for both timeout and cancellation)
                try {
                    await Cancel();
                } catch {
                    // Best effort - ignore if cancel API call fails
                }

                // Check what actually happened - cancellation or timeout
                cancellationToken.ThrowIfCancellationRequested();
                throw new TimeoutException($"Job timed out after {options.timeout} seconds.");
            }
        } else {
            await task;
        }

        lock (syncLock) {
            return jobOutput!.Deserialize<T>()!;
        }
    }



    /// <summary>
    /// Streams job output chunks as they become available.
    /// Useful for long-running jobs that produce incremental results (e.g., LLM text generation).
    /// </summary>
    /// <typeparam name="T">Expected chunk type.</typeparam>
    /// <param name="updateDelay">Delay between polling requests in milliseconds. Default: 1000ms.</param>
    /// <returns>Async enumerable of output chunks.</returns>
    public async IAsyncEnumerable<T> Stream<T>(int updateDelay = 1000) {
        while (true) {
            await Task.Delay(updateDelay);

            var response = await client.GetAsync<JsonObject>(
                $"v2/{endpointId}/stream/{jobId}",
                apiKey: apiKey
            );

            if (!IsFinal(response["status"]!.ToString()) || response.ContainsKey("stream")) {
                var streamArray = response["stream"]?.AsArray();
                if (streamArray != null) {
                    foreach (var chunk in streamArray) {
                        yield return chunk!["output"]!.Deserialize<T>()!;
                    }
                }
            } else if (IsFinal(response["status"]!.ToString())) {
                break;
            }
        }
    }



    /// <summary>
    /// Requests cancellation of the running job.
    /// </summary>
    /// <returns>Cancellation result from the API.</returns>
    public async Task<object?> Cancel() {
        return await client.PostAsync<object>($"v2/{endpointId}/cancel/{jobId}", apiKey: apiKey);
    }



    /// <summary>
    /// Checks if a job status represents a final state (completed or error).
    /// </summary>
    /// <param name="status">Job status string.</param>
    /// <returns>True if status is COMPLETED, FAILED, TIMED_OUT, or CANCELLED.</returns>
    public static bool IsFinal(string status) {
        return IsCompleted(status) || IsError(status);
    }



    /// <summary>
    /// Checks if a job status represents an error state.
    /// </summary>
    /// <param name="status">Job status string.</param>
    /// <returns>True if status is FAILED, TIMED_OUT, or CANCELLED.</returns>
    public static bool IsError(string status) {
        return new HashSet<string> { "FAILED", "TIMED_OUT", "CANCELLED" }.Contains(status);
    }



    /// <summary>
    /// Checks if a job status represents successful completion.
    /// </summary>
    /// <param name="status">Job status string.</param>
    /// <returns>True if status is COMPLETED.</returns>
    public static bool IsCompleted(string status) {
        return status == "COMPLETED";
    }
}
