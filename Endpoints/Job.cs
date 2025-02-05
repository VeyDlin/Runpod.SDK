using Newtonsoft.Json.Linq;

namespace Runpod.SDK.Endpoints;


public class Job {
    private string endpointId;
    private string jobId;
    private RunpodHttpClient client;

    private string? jobStatus;
    private JToken? jobOutput;
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

        var response = await client.GetAsync<JToken>($"v2/{endpointId}/status/{jobId}");
        var status = response["status"]!.ToString();

        if (IsFinal(status)) {
            lock (syncLock) {
                jobOutput = response["output"]!;
                jobStatus = response["status"]!.ToString();
            }
        }

        return status;
    }



    public async Task<T> Output<T>(int updateDelay = 500, int timeout = 0) {
        if (jobOutput is not null) {
            return jobOutput!.ToObject<T>()!;
        }

        var waitForCompletion = async () => {
            while (true) {
                var response = await client.GetAsync<JToken>($"v2/{endpointId}/status/{jobId}");
                if (!IsFinal(response["status"]!.ToString())) {
                    await Task.Delay(updateDelay);
                    continue;
                }
                lock (syncLock) {
                    jobOutput = response["output"]!;
                    jobStatus = response["status"]!.ToString();
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

        return jobOutput!.ToObject<T>()!;
    }



    public async IAsyncEnumerable<T> Stream<T>(int updateDelay = 500) {
        while (true) {
            await Task.Delay(updateDelay);

            var response = await client.GetAsync<JObject>($"v2/{endpointId}/stream/{jobId}");

            if (!IsFinal(response["status"]!.ToString()) || response.ContainsKey("stream")) {
                foreach (var chunk in response["stream"]!) {
                    yield return chunk["output"]!.ToObject<T>()!;
                }
            } else if (IsFinal(response["status"]!.ToString())) {
                break;
            }
        }
    }



    public async Task<object?> Cancel() {
        return await client.PostAsync<object>($"v2/{endpointId}/cancel/{jobId}");
    }



    private static bool IsFinal(string status) {
        return new HashSet<string> { "COMPLETED", "FAILED", "TIMED_OUT", "CANCELLED" }.Contains(status);
    }
}