using System.Text.Json.Nodes;

namespace Runpod.SDK.API;


public class Commands {
    private RunpodHttpClient client;


    internal Commands(RunpodHttpClient client) {
        this.client = client;
    }



    // Fetch user information
    public async Task<JsonNode> GetUser() {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Queries.User);
        return rawResponse["data"]!["myself"]!;
    }



    // Update user settings
    public async Task<JsonNode> UpdateUserSettings(string pubKey) {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Mutations.GenerateUserMutation(pubKey));
        return rawResponse["data"]!["updateUserSettings"]!;
    }



    // Fetch all GPUs
    public async Task<JsonNode> GetGpus() {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Queries.GpuTypes);
        return rawResponse["data"]!["gpuTypes"]!;
    }



    // Fetch a specific GPU
    public async Task<JsonNode> GetGpu(string gpuId, int gpuQuantity = 1) {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Queries.GenerateGpuQuery(gpuId, gpuQuantity));
        var gpus = rawResponse["data"]!["gpuTypes"]!;

        if (gpus is null || (gpus is JsonArray jsonArray && jsonArray.Count < 1)) {
            throw new ArgumentException("No GPU found with the specified ID. Use GetGpus() to see available GPUs.");
        }

        return gpus[0]!;
    }



    // Fetch all pods
    public async Task<JsonNode> GetPods() {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Queries.Pod);
        return rawResponse["data"]!["myself"]!["pods"]!;
    }



    // Fetch a specific pod
    public async Task<JsonNode> GetPod(string podId) {
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(Queries.GeneratePodQuery(podId));
        return rawResponse["data"]!["pod"]!;
    }



    // Create a pod
    public async Task<JsonNode> CreatePod(
        string name,
        string imageName,
        string gpuTypeId,
        string cloudType = "ALL",
        bool supportPublicIp = true,
        bool startSsh = true,
        string? dataCenterId = null,
        string? countryCode = null,
        int gpuCount = 1,
        int volumeInGb = 0,
        int? containerDiskInGb = null,
        int minVcpuCount = 1,
        int minMemoryInGb = 1,
        string dockerArgs = "",
        string? ports = null,
        string volumeMountPath = "/runpod-volume",
        Dictionary<string, string>? env = null,
        string? templateId = null,
        string? networkVolumeId = null,
        List<string>? allowedCudaVersions = null,
        int? minDownload = null,
        int? minUpload = null
    ) {
        // Validate GPU exists
        await GetGpu(gpuTypeId);

        // Validate cloudType
        if (!new[] { "ALL", "COMMUNITY", "SECURE" }.Contains(cloudType)) {
            throw new ArgumentException("cloudType must be one of ALL, COMMUNITY, or SECURE.");
        }

        var mutation = Mutations.GeneratePodDeploymentMutation(
            name, imageName, gpuTypeId, cloudType, supportPublicIp, startSsh,
            dataCenterId, countryCode, gpuCount, volumeInGb, containerDiskInGb,
            minVcpuCount, minMemoryInGb, dockerArgs, ports, volumeMountPath,
            env, templateId, networkVolumeId, allowedCudaVersions, minDownload, minUpload
        );

        var rawResponse = await client.RunGraphQLAsync<JsonNode>(mutation);
        return rawResponse["data"]!["podFindAndDeployOnDemand"]!;
    }



    // Stop a pod
    public async Task<JsonNode> StopPod(string podId) {
        var mutation = Mutations.GeneratePodStopMutation(podId);
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(mutation);
        return rawResponse["data"]!["podStop"]!;
    }



    // Resume a pod
    public async Task<JsonNode> ResumePod(string podId, int gpuCount) {
        var mutation = Mutations.GeneratePodResumeMutation(podId, gpuCount);
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(mutation);
        return rawResponse["data"]!["podResume"]!;
    }



    // Terminate a pod
    public async Task TerminatePod(string podId) {
        var mutation = Mutations.GeneratePodTerminateMutation(podId);
        await client.RunGraphQLAsync<JsonNode>(mutation);
    }



    // Create a container registry authentication
    public async Task<JsonNode> CreateContainerRegistryAuth(string name, string username, string password) {
        var mutation = Mutations.GenerateContainerRegistryAuth(name, username, password);
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(mutation);
        return rawResponse["data"]!["saveRegistryAuth"]!;
    }



    // Update a container registry authentication
    public async Task<JsonNode> UpdateContainerRegistryAuth(string registryAuthId, string username, string password) {
        var mutation = Mutations.UpdateContainerRegistryAuth(registryAuthId, username, password);
        var rawResponse = await client.RunGraphQLAsync<JsonNode>(mutation);
        return rawResponse["data"]!["updateRegistryAuth"]!;
    }



    // Delete a container registry authentication
    public async Task DeleteContainerRegistryAuth(string registryAuthId) {
        var mutation = Mutations.DeleteContainerRegistryAuth(registryAuthId);
        await client.RunGraphQLAsync<JsonNode>(mutation);
    }
}
