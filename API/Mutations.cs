namespace Runpod.SDK.API;


/// <summary>
/// Contains GraphQL mutation generators for RunPod API write operations.
/// </summary>
internal static class Mutations {
    /// <summary>
    /// Generates a GraphQL mutation to update user settings (SSH public key).
    /// </summary>
    /// <param name="pubKey">SSH public key to set for the user.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GenerateUserMutation(string pubKey) {
        var escapedPubKey = pubKey.Replace("\n", "\\n");
        return $@"
            mutation {{
                updateUserSettings(
                    input: {{
                        pubKey: ""{escapedPubKey}""
                    }}
                ) {{
                    id
                    pubKey
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to create a pod template.
    /// </summary>
    /// <param name="name">Template name.</param>
    /// <param name="imageName">Docker image to use.</param>
    /// <param name="dockerStartCmd">Docker container start command. Optional.</param>
    /// <param name="containerDiskInGb">Container disk size in GB. Default: 10.</param>
    /// <param name="volumeInGb">Network volume size in GB. Optional.</param>
    /// <param name="volumeMountPath">Volume mount path. Optional.</param>
    /// <param name="ports">Ports to expose (e.g., "8888/http"). Optional.</param>
    /// <param name="env">Environment variables. Optional.</param>
    /// <param name="isServerless">Whether this template is for serverless endpoints. Default: false.</param>
    /// <param name="registryAuthId">Container registry authentication ID. Optional.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GeneratePodTemplate(
        string name,
        string imageName,
        string? dockerStartCmd = null,
        int containerDiskInGb = 10,
        int? volumeInGb = null,
        string? volumeMountPath = null,
        string? ports = null,
        Dictionary<string, string>? env = null,
        bool isServerless = false,
        string? registryAuthId = null
    ) {
        var inputFields = new List<string> {
            $"name: \"{name}\"",
            $"imageName: \"{imageName}\"",
            $"containerDiskInGb: {containerDiskInGb}"
        };

        // Handle optional fields
        inputFields.Add(dockerStartCmd != null
            ? $"dockerArgs: \"{dockerStartCmd.Replace("\"", "\\\"")}\""
            : "dockerArgs: \"\"");

        inputFields.Add(volumeInGb.HasValue
            ? $"volumeInGb: {volumeInGb.Value}"
            : "volumeInGb: 0");

        if (!string.IsNullOrEmpty(volumeMountPath)) {
            inputFields.Add($"volumeMountPath: \"{volumeMountPath}\"");
        }

        inputFields.Add(!string.IsNullOrEmpty(ports)
            ? $"ports: \"{ports.Replace(" ", "")}\""
            : "ports: \"\"");

        if (env != null) {
            var envString = string.Join(", ", env.Select(kv => $"{{ key: \"{kv.Key}\", value: \"{kv.Value}\" }}"));
            inputFields.Add($"env: [{envString}]");
        } else {
            inputFields.Add("env: []");
        }

        inputFields.Add($"isServerless: {isServerless.ToString().ToLower()}");

        if (!string.IsNullOrEmpty(registryAuthId)) {
            inputFields.Add($"containerRegistryAuthId: \"{registryAuthId}\"");
        } else {
            inputFields.Add("containerRegistryAuthId: \"\"");
        }

        inputFields.AddRange(new[] {
            "startSsh: true",
            "isPublic: false",
            "readme: \"\""
        });

        var inputFieldsString = string.Join(", ", inputFields);

        return $@"
            mutation {{
                saveTemplate(
                    input: {{
                        {inputFieldsString}
                    }}
                ) {{
                    id
                    name
                    imageName
                    dockerArgs
                    containerDiskInGb
                    volumeInGb
                    volumeMountPath
                    ports
                    env {{
                        key
                        value
                    }}
                    isServerless
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to create and deploy a new on-demand GPU pod.
    /// </summary>
    /// <param name="name">Pod name.</param>
    /// <param name="imageName">Docker image to use.</param>
    /// <param name="gpuTypeId">GPU type identifier.</param>
    /// <param name="cloudType">Cloud type: ALL, COMMUNITY, or SECURE. Default: ALL.</param>
    /// <param name="supportPublicIp">Enable public IP address. Default: true.</param>
    /// <param name="startSsh">Start SSH service. Default: true.</param>
    /// <param name="dataCenterId">Specific data center ID. Optional.</param>
    /// <param name="countryCode">Country code for pod location. Optional.</param>
    /// <param name="gpuCount">Number of GPUs to attach. Optional.</param>
    /// <param name="volumeInGb">Network volume size in GB. Optional.</param>
    /// <param name="containerDiskInGb">Container disk size in GB. Optional.</param>
    /// <param name="minVcpuCount">Minimum vCPU count. Optional.</param>
    /// <param name="minMemoryInGb">Minimum memory in GB. Optional.</param>
    /// <param name="dockerArgs">Docker container arguments. Optional.</param>
    /// <param name="ports">Ports to expose (e.g., "8888/http"). Optional.</param>
    /// <param name="volumeMountPath">Volume mount path. Optional.</param>
    /// <param name="env">Environment variables. Optional.</param>
    /// <param name="templateId">Template ID to use. Optional.</param>
    /// <param name="networkVolumeId">Network volume ID. Optional.</param>
    /// <param name="allowedCudaVersions">Allowed CUDA versions. Optional.</param>
    /// <param name="minDownload">Minimum download speed in Mbps. Optional.</param>
    /// <param name="minUpload">Minimum upload speed in Mbps. Optional.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GeneratePodDeploymentMutation(
        string name,
        string imageName,
        string gpuTypeId,
        string cloudType = "ALL",
        bool supportPublicIp = true,
        bool startSsh = true,
        string? dataCenterId = null,
        string? countryCode = null,
        int? gpuCount = null,
        int? volumeInGb = null,
        int? containerDiskInGb = null,
        int? minVcpuCount = null,
        int? minMemoryInGb = null,
        string? dockerArgs = null,
        string? ports = null,
        string? volumeMountPath = null,
        Dictionary<string, string>? env = null,
        string? templateId = null,
        string? networkVolumeId = null,
        List<string>? allowedCudaVersions = null,
        int? minDownload = null,
        int? minUpload = null
    ) {
        var inputFields = new List<string> {
            $"name: \"{name}\"",
            $"imageName: \"{imageName}\"",
            $"gpuTypeId: \"{gpuTypeId}\"",
            $"cloudType: {cloudType}"
        };

        if (startSsh) {
            inputFields.Add("startSsh: true");
        }

        inputFields.Add($"supportPublicIp: {supportPublicIp.ToString().ToLower()}");

        // Optional fields
        if (!string.IsNullOrEmpty(dataCenterId)) {
            inputFields.Add($"dataCenterId: \"{dataCenterId}\"");
        }

        if (!string.IsNullOrEmpty(countryCode)) {
            inputFields.Add($"countryCode: \"{countryCode}\"");
        }

        if (gpuCount.HasValue) {
            inputFields.Add($"gpuCount: {gpuCount}");
        }

        if (volumeInGb.HasValue) {
            inputFields.Add($"volumeInGb: {volumeInGb}");
        }

        if (containerDiskInGb.HasValue) {
            inputFields.Add($"containerDiskInGb: {containerDiskInGb}");
        }

        if (minVcpuCount.HasValue) {
            inputFields.Add($"minVcpuCount: {minVcpuCount}");
        }

        if (minMemoryInGb.HasValue) {
            inputFields.Add($"minMemoryInGb: {minMemoryInGb}");
        }

        if (!string.IsNullOrEmpty(dockerArgs)) {
            inputFields.Add($"dockerArgs: \"{dockerArgs}\"");
        }

        if (!string.IsNullOrEmpty(ports)) {
            inputFields.Add($"ports: \"{ports.Replace(" ", "")}\"");
        }

        if (!string.IsNullOrEmpty(volumeMountPath)) {
            inputFields.Add($"volumeMountPath: \"{volumeMountPath}\"");
        }

        if (env != null) {
            var envString = string.Join(", ", env.Select(kv => $"{{ key: \"{kv.Key}\", value: \"{kv.Value}\" }}"));
            inputFields.Add($"env: [{envString}]");
        }

        if (!string.IsNullOrEmpty(templateId)) {
            inputFields.Add($"templateId: \"{templateId}\"");
        }

        if (!string.IsNullOrEmpty(networkVolumeId)) {
            inputFields.Add($"networkVolumeId: \"{networkVolumeId}\"");
        }

        if (allowedCudaVersions != null && allowedCudaVersions.Any()) {
            var cudaString = string.Join(", ", allowedCudaVersions.Select(version => $"\"{version}\""));
            inputFields.Add($"allowedCudaVersions: [{cudaString}]");
        }

        if (minDownload.HasValue) {
            inputFields.Add($"minDownload: {minDownload}");
        }

        if (minUpload.HasValue) {
            inputFields.Add($"minUpload: {minUpload}");
        }

        var inputString = string.Join(", ", inputFields);
        return $@"
            mutation {{
                podFindAndDeployOnDemand(
                    input: {{
                        {inputString}
                    }}
                ) {{
                    id
                    desiredStatus
                    imageName
                    env
                    machineId
                    machine {{
                        podHostId
                    }}
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to stop a running pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GeneratePodStopMutation(string podId) {
        return $@"
            mutation {{
                podStop(input: {{ podId: ""{podId}"" }}) {{
                    id
                    desiredStatus
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to resume a stopped pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <param name="gpuCount">Number of GPUs to attach.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GeneratePodResumeMutation(string podId, int gpuCount) {
        return $@"
            mutation {{
                podResume(input: {{ podId: ""{podId}"", gpuCount: {gpuCount} }}) {{
                    id
                    desiredStatus
                    imageName
                    env
                    machineId
                    machine {{
                        podHostId
                    }}
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to permanently terminate a pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GeneratePodTerminateMutation(string podId) {
        return $@"
            mutation {{
                podTerminate(input: {{ podId: ""{podId}"" }})
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to create a serverless endpoint.
    /// </summary>
    /// <param name="name">Endpoint name.</param>
    /// <param name="templateId">Template ID to use.</param>
    /// <param name="gpuIds">GPU type identifier. Default: AMPERE_16.</param>
    /// <param name="networkVolumeId">Network volume ID. Optional.</param>
    /// <param name="locations">Preferred locations. Optional.</param>
    /// <param name="idleTimeout">Idle timeout in seconds. Default: 5.</param>
    /// <param name="scalerType">Scaler type (e.g., QUEUE_DELAY). Default: QUEUE_DELAY.</param>
    /// <param name="scalerValue">Scaler value. Default: 4.</param>
    /// <param name="workersMin">Minimum workers. Default: 0.</param>
    /// <param name="workersMax">Maximum workers. Default: 3.</param>
    /// <param name="flashboot">Enable flashboot. Default: false.</param>
    /// <param name="allowedCudaVersions">Allowed CUDA versions. Default: 12.1,12.2,12.3,12.4,12.5.</param>
    /// <param name="gpuCount">Number of GPUs per worker. Optional.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GenerateEndpointMutation(
        string name,
        string templateId,
        string gpuIds = "AMPERE_16",
        string? networkVolumeId = null,
        string? locations = null,
        int idleTimeout = 5,
        string scalerType = "QUEUE_DELAY",
        int scalerValue = 4,
        int workersMin = 0,
        int workersMax = 3,
        bool flashboot = false,
        string allowedCudaVersions = "12.1,12.2,12.3,12.4,12.5",
        int? gpuCount = null
    ) {
        var inputFields = new List<string>();

        // Required Fields
        if (flashboot) {
            name += "-fb";
        }

        inputFields.Add($"name: \"{name}\"");
        inputFields.Add($"templateId: \"{templateId}\"");
        inputFields.Add($"gpuIds: \"{gpuIds}\"");

        // Optional Fields
        inputFields.Add($"networkVolumeId: \"{networkVolumeId ?? ""}\"");
        inputFields.Add($"locations: \"{locations ?? ""}\"");
        inputFields.Add($"idleTimeout: {idleTimeout}");
        inputFields.Add($"scalerType: \"{scalerType}\"");
        inputFields.Add($"scalerValue: {scalerValue}");
        inputFields.Add($"workersMin: {workersMin}");
        inputFields.Add($"workersMax: {workersMax}");
        inputFields.Add($"allowedCudaVersions: \"{allowedCudaVersions}\"");

        if (gpuCount.HasValue) {
            inputFields.Add($"gpuCount: {gpuCount.Value}");
        }

        var inputFieldsString = string.Join(", ", inputFields);

        return $@"
            mutation {{
                saveEndpoint(
                    input: {{
                        {inputFieldsString}
                    }}
                ) {{
                    id
                    name
                    templateId
                    gpuIds
                    networkVolumeId
                    locations
                    idleTimeout
                    scalerType
                    scalerValue
                    workersMin
                    workersMax
                    allowedCudaVersions
                    gpuCount
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to update an endpoint's template.
    /// </summary>
    /// <param name="endpointId">Endpoint identifier.</param>
    /// <param name="templateId">New template ID.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string UpdateEndpointTemplateMutation(string endpointId, string templateId) {
        var inputFields = new List<string> {
            $"templateId: \"{templateId}\"",
            $"endpointId: \"{endpointId}\""
        };

        var inputFieldsString = string.Join(", ", inputFields);

        return $@"
            mutation {{
                updateEndpointTemplate(input: {{{inputFieldsString}}}) {{
                    id
                    templateId
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to create container registry authentication credentials.
    /// </summary>
    /// <param name="name">Registry name.</param>
    /// <param name="username">Registry username.</param>
    /// <param name="password">Registry password.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string GenerateContainerRegistryAuth(string name, string username, string password) {
        var inputFields = new List<string> {
            $"name: \"{name}\"",
            $"username: \"{username}\"",
            $"password: \"{password}\""
        };

        var inputString = string.Join(", ", inputFields);

        return $@"
            mutation SaveRegistryAuth {{
                saveRegistryAuth(input: {{{inputString}}}) {{
                    id
                    name
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to update existing container registry authentication credentials.
    /// </summary>
    /// <param name="registryAuthId">Registry authentication ID.</param>
    /// <param name="username">New username.</param>
    /// <param name="password">New password.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string UpdateContainerRegistryAuth(string registryAuthId, string username, string password) {
        var inputFields = new List<string> {
            $"id: \"{registryAuthId}\"",
            $"username: \"{username}\"",
            $"password: \"{password}\""
        };

        var inputString = string.Join(", ", inputFields);

        return $@"
            mutation UpdateRegistryAuth {{
                updateRegistryAuth(input: {{{inputString}}}) {{
                    id
                    name
                }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL mutation to delete container registry authentication credentials.
    /// </summary>
    /// <param name="registryAuthId">Registry authentication ID.</param>
    /// <returns>GraphQL mutation string.</returns>
    public static string DeleteContainerRegistryAuth(string registryAuthId) {
        return $@"
            mutation DeleteRegistryAuth {{
                deleteRegistryAuth(registryAuthId: \""{{registryAuthId}}\"")
            }}
        ";
    }
}
