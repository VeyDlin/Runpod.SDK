# SDK for the runpod.io API

## Initializing the Client

```csharp
var runpod = new RunpodClient("api key");
```

## Working with Serverless Endpoints

### Create an Endpoint

```csharp
var endpoint = runpod.Endpoint("id");
```

### Run a Synchronous Task

```csharp
var runSyncResult = await RunSync<dynamic>(new {
    prompt = "Hello World"
});
```

### Run an Asynchronous Task and Wait for its Completion

```csharp
var runJob = await Run(new {
    prompt = "Hello World"
});
var runResult = await runJob.Output<dynamic>();
```

### Stream Synchronous Task Output
Want to get LLM output in real-time? It would look something like this:
```csharp
await foreach (var item in runJob.Stream<string>()) {
    Console.WriteLine(item);
}
```

### Get Task Status
```csharp
var status = await runJob.Status();
```

### Cancel a Task
```csharp
await runJob.Cancel();
```

### Check Endpoint Health

```csharp
var health = await endpoint.Health();
```

### Perform Purge
```csharp
await endpoint.Purge();
```

### Perform Purge Queue
```csharp
await endpoint.PurgeQueue();
```

## Working with Profile and Pods

### Fetch User Information
```csharp
var user = await runpod.cmd.GetUser();
```

### Update User Settings
```csharp
var updatedSettings = await runpod.cmd.UpdateUserSettings("publicKey");
```

### Fetch All GPUs
```csharp
var gpus = await runpod.cmd.GetGpus();
```

### Fetch a Specific GPU
```csharp
var gpu = await runpod.cmd.GetGpu("gpuId");
```

### Fetch All Pods
```csharp
var pods = await runpod.cmd.GetPods();
```

### Fetch a Specific Pod
```csharp
var pod = await runpod.cmd.GetPod("podId");
```

### Create a Pod
```csharp
var newPod = await runpod.cmd.CreatePod(
    name: "Pod Name",
    imageName: "Image Name",
    gpuTypeId: "gpuTypeId",
    cloudType: "ALL",
    supportPublicIp: true,
    startSsh: true,
    dataCenterId: null,
    countryCode: null,
    gpuCount: 1,
    volumeInGb: 0,
    containerDiskInGb: null,
    minVcpuCount: 1,
    minMemoryInGb: 1,
    dockerArgs: "",
    ports: null,
    volumeMountPath: "/runpod-volume",
    env: null,
    templateId: null,
    networkVolumeId: null,
    allowedCudaVersions: null,
    minDownload: null,
    minUpload: null
);
```

### Stop a Pod
```csharp
var stoppedPod = await runpod.cmd.StopPod("podId");
```

### Resume a Pod
```csharp
var resumedPod = await runpod.cmd.ResumePod("podId", gpuCount: 1);
```

### Terminate a Pod
```csharp
await runpod.cmd.TerminatePod("podId");
```

### Create Container Registry Authentication
```csharp
var registryAuth = await runpod.cmd.CreateContainerRegistryAuth("name", "username", "password");
```

### Update Container Registry Authentication
```csharp
var updatedRegistryAuth = await runpod.cmd.UpdateContainerRegistryAuth("registryAuthId", "username", "password");
```

### Delete Container Registry Authentication
```csharp
await runpod.cmd.DeleteContainerRegistryAuth("registryAuthId");
```