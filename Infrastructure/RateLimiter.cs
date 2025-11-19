using System.Collections.Concurrent;

namespace Runpod.SDK.Infrastructure;

internal class RateLimiter {
    private readonly ConcurrentDictionary<string, EndpointRateLimiter> limiters = new();

    public async Task<T> ExecuteAsync<T>(string endpoint, Func<Task<T>> operation) {
        var limiter = limiters.GetOrAdd(endpoint, _ => {
            var config = RateLimitConfiguration.GetLimitForEndpoint(endpoint);
            return new EndpointRateLimiter(config);
        });

        return await limiter.ExecuteAsync(operation);
    }

    private class EndpointRateLimiter {
        private readonly RateLimitConfiguration config;
        private readonly SemaphoreSlim concurrencyLimiter;
        private readonly Queue<DateTime> requestTimestamps = new();
        private readonly object timestampLock = new();

        public EndpointRateLimiter(RateLimitConfiguration config) {
            this.config = config;
            this.concurrencyLimiter = new SemaphoreSlim(config.maxConcurrent, config.maxConcurrent);
        }

        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation) {
            await WaitForRateLimitWindow();

            await concurrencyLimiter.WaitAsync();
            try {
                lock (timestampLock) {
                    requestTimestamps.Enqueue(DateTime.UtcNow);
                }

                return await operation();
            } finally {
                concurrencyLimiter.Release();
            }
        }

        private async Task WaitForRateLimitWindow() {
            while (true) {
                TimeSpan? delayTime = null;

                lock (timestampLock) {
                    var windowStart = DateTime.UtcNow.AddSeconds(-config.windowSizeSeconds);

                    while (requestTimestamps.Count > 0 && requestTimestamps.Peek() < windowStart) {
                        requestTimestamps.Dequeue();
                    }

                    if (requestTimestamps.Count < config.requestsPerWindow) {
                        return;
                    }

                    var oldestRequest = requestTimestamps.Peek();
                    var waitTime = oldestRequest.AddSeconds(config.windowSizeSeconds) - DateTime.UtcNow;

                    if (waitTime > TimeSpan.Zero) {
                        delayTime = waitTime;
                    }
                }

                if (delayTime.HasValue) {
                    Console.WriteLine($"Rate limit approaching, waiting {delayTime.Value.TotalMilliseconds}ms");
                    await Task.Delay(delayTime.Value);
                }
            }
        }
    }
}