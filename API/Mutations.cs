namespace Runpod.SDK.API;


internal static class Mutations {
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



    public static string GeneratePodTerminateMutation(string podId) {
        return $@"
            mutation {{
                podTerminate(input: {{ podId: ""{podId}"" }})
            }}
        ";
    }



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



    public static string DeleteContainerRegistryAuth(string registryAuthId) {
        return $@"
            mutation DeleteRegistryAuth {{
                deleteRegistryAuth(registryAuthId: \""{{registryAuthId}}\"")
            }}
        ";
    }
}
