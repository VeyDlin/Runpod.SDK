using System.Text.Json.Nodes;

namespace Runpod.SDK.API;


/// <summary>
/// Provides methods for managing RunPod resources via GraphQL API.
/// Includes pod management, GPU queries, and user settings.
/// </summary>
public class Commands {
    private GraphQLClient graphql;


    internal Commands(RunpodHttpClient client, string apiKey) {
        graphql = client.CreateGraphQLClient(apiKey);
    }



    /// <summary>
    /// Fetches current user information including network volumes and SSH keys.
    /// </summary>
    /// <returns>User information as JSON node.</returns>
    public async Task<JsonNode> GetUser() {
        return await graphql.ExecuteAndExtractAsync(Queries.User, "data.myself");
    }



    /// <summary>
    /// Updates user settings, such as SSH public key.
    /// </summary>
    /// <param name="pubKey">SSH public key to set for the user.</param>
    /// <returns>Updated user settings as JSON node.</returns>
    public async Task<JsonNode> UpdateUserSettings(string pubKey) {
        return await graphql.ExecuteAndExtractAsync(
            Mutations.GenerateUserMutation(pubKey),
            "data.updateUserSettings"
        );
    }



    /// <summary>
    /// Fetches all available GPU types with their specifications.
    /// </summary>
    /// <returns>Array of GPU types as JSON node.</returns>
    public async Task<JsonNode> GetGpus() {
        return await graphql.ExecuteAndExtractAsync(Queries.GpuTypes, "data.gpuTypes");
    }



    /// <summary>
    /// Fetches detailed information about a specific GPU type.
    /// </summary>
    /// <param name="gpuId">GPU type identifier (e.g., "NVIDIA RTX A6000").</param>
    /// <param name="gpuQuantity">Number of GPUs to query pricing for. Default: 1.</param>
    /// <returns>GPU information as JSON node.</returns>
    /// <exception cref="ArgumentException">Thrown when GPU ID is not found.</exception>
    public async Task<JsonNode> GetGpu(string gpuId, int gpuQuantity = 1) {
        var gpus = await graphql.ExecuteAndExtractAsync(
            Queries.GenerateGpuQuery(gpuId, gpuQuantity),
            "data.gpuTypes"
        );

        if (gpus is null || (gpus is JsonArray jsonArray && jsonArray.Count < 1)) {
            throw new ArgumentException("No GPU found with the specified ID. Use GetGpus() to see available GPUs.");
        }

        return gpus[0]!;
    }



    /// <summary>
    /// Fetches all pods owned by the current user.
    /// </summary>
    /// <returns>Array of pods as JSON node.</returns>
    public async Task<JsonNode> GetPods() {
        return await graphql.ExecuteAndExtractAsync(Queries.Pod, "data.myself.pods");
    }



    /// <summary>
    /// Fetches detailed information about a specific pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <returns>Pod information as JSON node.</returns>
    public async Task<JsonNode> GetPod(string podId) {
        return await graphql.ExecuteAndExtractAsync(
            Queries.GeneratePodQuery(podId),
            "data.pod"
        );
    }



    /// <summary>
    /// Creates a new on-demand GPU pod with specified configuration.
    /// </summary>
    /// <param name="name">Pod name.</param>
    /// <param name="imageName">Docker image to use.</param>
    /// <param name="gpuTypeId">GPU type identifier.</param>
    /// <param name="cloudType">Cloud type: ALL, COMMUNITY, or SECURE. Default: ALL.</param>
    /// <param name="supportPublicIp">Enable public IP address. Default: true.</param>
    /// <param name="startSsh">Start SSH service. Default: true.</param>
    /// <param name="dataCenterId">Specific data center ID. Optional.</param>
    /// <param name="countryCode">Country code for pod location. Optional.</param>
    /// <param name="gpuCount">Number of GPUs to attach. Default: 1.</param>
    /// <param name="volumeInGb">Network volume size in GB. Default: 0.</param>
    /// <param name="containerDiskInGb">Container disk size in GB. Optional.</param>
    /// <param name="minVcpuCount">Minimum vCPU count. Default: 1.</param>
    /// <param name="minMemoryInGb">Minimum memory in GB. Default: 1.</param>
    /// <param name="dockerArgs">Docker container arguments. Default: empty.</param>
    /// <param name="ports">Ports to expose (e.g., "8888/http"). Optional.</param>
    /// <param name="volumeMountPath">Volume mount path. Default: /runpod-volume.</param>
    /// <param name="env">Environment variables. Optional.</param>
    /// <param name="templateId">Template ID to use. Optional.</param>
    /// <param name="networkVolumeId">Network volume ID. Optional.</param>
    /// <param name="allowedCudaVersions">Allowed CUDA versions. Optional.</param>
    /// <param name="minDownload">Minimum download speed in Mbps. Optional.</param>
    /// <param name="minUpload">Minimum upload speed in Mbps. Optional.</param>
    /// <returns>Created pod information as JSON node.</returns>
    /// <exception cref="ArgumentException">Thrown when cloudType is invalid or GPU not found.</exception>
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

        return await graphql.ExecuteAndExtractAsync(mutation, "data.podFindAndDeployOnDemand");
    }



    /// <summary>
    /// Stops a running pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <returns>Pod status as JSON node.</returns>
    public async Task<JsonNode> StopPod(string podId) {
        var mutation = Mutations.GeneratePodStopMutation(podId);
        return await graphql.ExecuteAndExtractAsync(mutation, "data.podStop");
    }



    /// <summary>
    /// Resumes a stopped pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <param name="gpuCount">Number of GPUs to attach.</param>
    /// <returns>Pod status as JSON node.</returns>
    public async Task<JsonNode> ResumePod(string podId, int gpuCount) {
        var mutation = Mutations.GeneratePodResumeMutation(podId, gpuCount);
        return await graphql.ExecuteAndExtractAsync(mutation, "data.podResume");
    }



    /// <summary>
    /// Permanently terminates a pod. This action cannot be undone.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    public async Task TerminatePod(string podId) {
        var mutation = Mutations.GeneratePodTerminateMutation(podId);
        await graphql.ExecuteAsync<JsonNode>(mutation);
    }



    /// <summary>
    /// Creates authentication credentials for a container registry.
    /// </summary>
    /// <param name="name">Registry name.</param>
    /// <param name="username">Registry username.</param>
    /// <param name="password">Registry password.</param>
    /// <returns>Created registry authentication as JSON node.</returns>
    public async Task<JsonNode> CreateContainerRegistryAuth(string name, string username, string password) {
        var mutation = Mutations.GenerateContainerRegistryAuth(name, username, password);
        return await graphql.ExecuteAndExtractAsync(mutation, "data.saveRegistryAuth");
    }



    /// <summary>
    /// Updates existing container registry authentication credentials.
    /// </summary>
    /// <param name="registryAuthId">Registry authentication ID.</param>
    /// <param name="username">New username.</param>
    /// <param name="password">New password.</param>
    /// <returns>Updated registry authentication as JSON node.</returns>
    public async Task<JsonNode> UpdateContainerRegistryAuth(string registryAuthId, string username, string password) {
        var mutation = Mutations.UpdateContainerRegistryAuth(registryAuthId, username, password);
        return await graphql.ExecuteAndExtractAsync(mutation, "data.updateRegistryAuth");
    }



    /// <summary>
    /// Deletes container registry authentication credentials.
    /// </summary>
    /// <param name="registryAuthId">Registry authentication ID.</param>
    public async Task DeleteContainerRegistryAuth(string registryAuthId) {
        var mutation = Mutations.DeleteContainerRegistryAuth(registryAuthId);
        await graphql.ExecuteAsync<JsonNode>(mutation);
    }
}
