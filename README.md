# RunPod SDK for .NET

A modern, type-safe .NET SDK for the [RunPod](https://runpod.io) API. Manage GPU pods, run serverless endpoints, and scale your AI workloads with ease.

[![NuGet](https://img.shields.io/nuget/v/Runpod.SDK.svg)](https://www.nuget.org/packages/Runpod.SDK/)
[![License](https://img.shields.io/github/license/VeyDlin/Runpod.SDK)](LICENSE)

## Features

**Full RunPod API Coverage** - Pods, GPUs, Serverless Endpoints, and more
**Dependency Injection Support** - First-class support for ASP.NET Core
**Multi-tenant Architecture** - Factory pattern for dynamic API keys
**Flexible Polling** - Adaptive strategies for real-time and batch jobs
**Cancellation Support** - Gracefully cancel long-running operations
**Resource Management** - Proper `IDisposable` implementation
**Type Safety** - Strongly-typed models with nullability support
**Rate Limiting** - Built-in protection against API limits

## Installation

```bash
dotnet add package Runpod.SDK
```

Or via NuGet Package Manager:

```
Install-Package Runpod.SDK
```

## Quick Start

### Basic Usage

```csharp
using Runpod.SDK;

using var runpod = new RunpodClient("your_api_key_here");

// Run serverless endpoint
var endpoint = runpod.Endpoint("your-endpoint-id");
var result = await endpoint.RunSync<dynamic>(new {
    prompt = "A beautiful sunset over mountains"
});

Console.WriteLine($"Result: {result}");
```

### ASP.NET Core - Single API Key

For applications using one RunPod account:

```csharp
// Program.cs
using Runpod.SDK;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRunpod(
    builder.Configuration["RunPod:ApiKey"]!
);

builder.Services.AddControllers();
var app = builder.Build();
app.MapControllers();
app.Run();
```

```csharp
// Controller
[ApiController]
[Route("api/[controller]")]
public class ImageController : ControllerBase {
    private readonly IRunpodClient _runpod;

    public ImageController(IRunpodClient runpod) {
        _runpod = runpod;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] ImageRequest request) {
        var endpoint = _runpod.Endpoint("your-endpoint-id");

        var result = await endpoint.RunSync<ImageResult>(
            new { prompt = request.Prompt },
            pollingOptions: new PollingOptions {
                updateDelay = 500  // Check every 500ms for fast feedback
            },
            cancellationToken: HttpContext.RequestAborted
        );

        return Ok(result);
    }
}
```

### ASP.NET Core - Multi-tenant (Factory)

For SaaS applications where each user has their own RunPod API key:

```csharp
// Program.cs
builder.Services.AddRunpodFactory();  // Register factory instead
builder.Services.AddDbContext<AppDbContext>();
```

```csharp
// Controller
public class ImageController : ControllerBase {
    private readonly IRunpodClientFactory _factory;
    private readonly AppDbContext _db;

    public ImageController(IRunpodClientFactory factory, AppDbContext db) {
        _factory = factory;
        _db = db;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] Request req) {
        // Get user's API key from database
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userSettings = await _db.UserSettings.FindAsync(userId);

        if (string.IsNullOrEmpty(userSettings?.RunpodApiKey)) {
            return BadRequest("RunPod API key not configured");
        }

        // Create client with user's key (shares HttpClient pool!)
        using var runpod = _factory.CreateClient(userSettings.RunpodApiKey);

        var endpoint = runpod.Endpoint(userSettings.PreferredEndpointId!);
        var result = await endpoint.RunSync<ImageResult>(
            new { prompt = req.Prompt },
            cancellationToken: HttpContext.RequestAborted
        );

        return Ok(result);
    }
}
```

## Core Concepts

### Serverless Endpoints

#### Synchronous Execution
Best for real-time tasks (image generation, LLM inference):

```csharp
var result = await endpoint.RunSync<ImageResult>(new {
    prompt = "Beautiful landscape"
});
```

#### Asynchronous Execution
For longer-running tasks:

```csharp
// Submit job
var job = await endpoint.Run(new {
    task = "process_video",
    url = "https://example.com/video.mp4"
});

// Wait for completion
var result = await job.Output<VideoResult>();

// Or check status periodically
var status = await job.Status();
```

#### Streaming Output
For LLM streaming responses:

```csharp
var job = await endpoint.Run(new { prompt = "Tell me a story" });

await foreach (var chunk in job.Stream<string>()) {
    Console.Write(chunk);  // Real-time streaming
}
```

### Polling Strategies

#### Real-time Tasks (Default)
Fast polling for immediate user feedback:

```csharp
var result = await endpoint.RunSync<Result>(input);  // Default: 1 second polling
```

#### Long-running Batch Jobs
Progressive backoff to reduce API calls:

```csharp
var job = await endpoint.Run(batchInput);

var result = await job.Output<BatchResult>(new PollingOptions {
    updateDelay = 1000,           	// Start with 1 second
    useProgressiveBackoff = true,  	// Enable adaptive polling
    maxDelay = 60000,             	// Max 60 seconds between checks
    timeout = 0                   	// No timeout (wait indefinitely)
});
```

### Cancellation

Gracefully cancel operations when users disconnect. **The SDK automatically cancels jobs on RunPod side** when cancellation is requested:

```csharp
[HttpPost("generate")]
public async Task<IActionResult> Generate([FromBody] Request req) {
    var result = await endpoint.RunSync<Result>(
        req,
        cancellationToken: HttpContext.RequestAborted  // Auto-cancel on disconnect
    );
    return Ok(result);
}
// When user disconnects:
// 1. Client stops polling
// 2. SDK automatically calls RunPod API to cancel the job
// 3. GPU resources are freed immediately
```

Manual cancellation:

```csharp
var cts = new CancellationTokenSource();

var task = job.Output<Result>(cancellationToken: cts.Token);

// Cancel after 30 seconds
await Task.Delay(30000);
cts.Cancel();  // SDK will call RunPod cancel API automatically
```

## Pod Management

### List Available GPUs

```csharp
var gpus = await runpod.cmd.GetGpus();
foreach (var gpu in gpus.AsArray()) {
    Console.WriteLine($"GPU: {gpu["displayName"]}, Memory: {gpu["memoryInGb"]}GB");
}
```

### Create a Pod

```csharp
var pod = await runpod.cmd.CreatePod(
    name: "training-pod",
    imageName: "runpod/pytorch:2.0.0",
    gpuTypeId: "NVIDIA RTX A6000",
    gpuCount: 2,
    volumeInGb: 50,
    env: new Dictionary<string, string> {
        ["WANDB_API_KEY"] = "your-wandb-key",
        ["HF_TOKEN"] = "your-hf-token"
    }
);

Console.WriteLine($"Pod created: {pod["id"]}");
```

### Manage Pods

```csharp
// List all pods
var pods = await runpod.cmd.GetPods();

// Get specific pod
var pod = await runpod.cmd.GetPod("pod-id");

// Stop pod
await runpod.cmd.StopPod("pod-id");

// Resume pod
await runpod.cmd.ResumePod("pod-id", gpuCount: 1);

// Terminate pod
await runpod.cmd.TerminatePod("pod-id");
```

## Advanced Features

### Endpoint Operations

```csharp
var endpoint = runpod.Endpoint("endpoint-id");

// Check health
var health = await endpoint.Health();

// Cancel a job
await job.Cancel();

// Purge queue
await endpoint.PurgeQueue();

// Full purge
await endpoint.Purge();
```

### Error Handling

```csharp
try {
    var result = await endpoint.RunSync<Result>(input);
} catch (JobErrorException ex) {
    Console.WriteLine($"Job failed: {ex.status} - {ex.Message}");
} catch (TimeoutException ex) {
    Console.WriteLine($"Job timed out: {ex.Message}");
} catch (QueryException ex) {
    Console.WriteLine($"GraphQL error: {ex.Message}");
} catch (OperationCanceledException) {
    Console.WriteLine("Operation was cancelled");
}
```

### User Management

```csharp
// Get user info
var user = await runpod.cmd.GetUser();
Console.WriteLine($"User ID: {user["id"]}");
Console.WriteLine($"Network Volumes: {user["networkVolumes"]}");

// Update SSH key
await runpod.cmd.UpdateUserSettings(publicKey);
```

### Container Registry

```csharp
// Add registry authentication
var auth = await runpod.cmd.CreateContainerRegistryAuth(
    name: "docker-hub",
    username: "myuser",
    password: "mypassword"
);

// Update credentials
await runpod.cmd.UpdateContainerRegistryAuth(
    registryAuthId: auth["id"]!.ToString(),
    username: "newuser",
    password: "newpassword"
);

// Delete
await runpod.cmd.DeleteContainerRegistryAuth(auth["id"]!.ToString());
```

## Configuration

### Dependency Injection Options

```csharp
builder.Services.AddRunpod(
    apiKey: configuration["RunPod:ApiKey"]!,
    configure: options => {
        options.enableRateLimiting = true;
        options.graphqlAddress = "https://api.runpod.io";
        options.endpointAddress = "https://api.runpod.ai";
    }
);
```

### Factory Options

```csharp
builder.Services.AddRunpodFactory(options => {
    options.enableRateLimiting = true;
    options.graphqlAddress = "https://api.runpod.io";
    options.endpointAddress = "https://api.runpod.ai";
});
```

## Documentation

- **[RunPod API Docs](https://docs.runpod.io/)** - Official RunPod documentation

## Best Practices

1. **Always use `using` for resource cleanup:**
   ```csharp
   using var runpod = new RunpodClient(apiKey);
   ```

2. **Use Factory for multi-tenant scenarios:**
   - Shared HttpClient pool = better performance
   - Each user gets their own client with their API key

3. **Choose appropriate polling strategy:**
   - Real-time tasks: Default (1 second)
   - Batch jobs: Progressive backoff

4. **Always pass CancellationToken in ASP.NET Core:**
   ```csharp
   cancellationToken: HttpContext.RequestAborted
   ```

5. **Handle errors appropriately:**
   - Catch `JobErrorException` for job failures
   - Catch `TimeoutException` for timeouts
   - Catch `OperationCanceledException` for cancellations

## Requirements

- .NET 9.0 or higher
- RunPod API key ([Get yours here](https://www.runpod.io/console/user/settings))

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- [Report Issues](https://github.com/VeyDlin/Runpod.SDK/issues)
- [Discussions](https://github.com/VeyDlin/Runpod.SDK/discussions)
- Contact: [Create an issue](https://github.com/VeyDlin/Runpod.SDK/issues/new)

## Acknowledgments

- [RunPod](https://runpod.io) - Cloud GPU platform
- [RunPod Python SDK](https://github.com/runpod/runpod-python) - Official Python SDK for reference

---

Made with ❤️ for the AI/ML community
