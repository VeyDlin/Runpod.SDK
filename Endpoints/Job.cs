using System.Text.Json;
using System.Text.Json.Nodes;

namespace Runpod.SDK.Endpoints;


public class Job {
    private string endpointId;
    private string jobId;
    private RunpodHttpClient client;

    private string? jobStatus;
    private JsonNode? jobOutput;
    private object syncLock = new();


    internal Job(RunpodHttpClient client, string endpointId, string jobId) {
        this.client = client;
        this.endpointId = endpointId;
        this.jobId = jobId;
    }



    public async Task<string> Status() {
        if (jobStatus is not null) {
            return jobStatus;
        }

        var response = await client.GetAsync<JsonNode>($"v2/{endpointId}/status/{jobId}");
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



    public async Task<T> Output<T>(int updateDelay = 1000, int timeout = 0) {
        if (jobOutput is not null) {
            return jobOutput!.Deserialize<T>()!;
        }

        var waitForCompletion = async () => {
            while (true) {
                var response = await client.GetAsync<JsonNode>($"v2/{endpointId}/status/{jobId}");
                if (!IsFinal(response["status"]!.ToString())) {
                    await Task.Delay(updateDelay);
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
        };

        var task = waitForCompletion();

        if (timeout > 0) {
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout));
            if (await Task.WhenAny(task, timeoutTask) == timeoutTask) {
                throw new TimeoutException("Job timed out.");
            }
        } else {
            await task;
        }

        return jobOutput!.Deserialize<T>()!;
    }



    public async IAsyncEnumerable<T> Stream<T>(int updateDelay = 1000) {
        while (true) {
            await Task.Delay(updateDelay);

            var response = await client.GetAsync<JsonObject>($"v2/{endpointId}/stream/{jobId}");

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



    public async Task<object?> Cancel() {
        return await client.PostAsync<object>($"v2/{endpointId}/cancel/{jobId}");
    }



    public static bool IsFinal(string status) {
        return IsCompleted(status) || IsError(status);
    }



    public static bool IsError(string status) {
        return new HashSet<string> { "FAILED", "TIMED_OUT", "CANCELLED" }.Contains(status);
    }



    public static bool IsCompleted(string status) {
        return status == "COMPLETED";
    }
}