namespace Runpod.SDK.API;


/// <summary>
/// Contains GraphQL query templates for RunPod API operations.
/// </summary>
internal static class Queries {
    /// <summary>
    /// GraphQL query to fetch all pods owned by the current user.
    /// </summary>
    public static readonly string Pod = @"
        query myPods {
            myself {
                pods {
                    id
                    containerDiskInGb
                    costPerHr
                    desiredStatus
                    dockerArgs
                    dockerId
                    env
                    gpuCount
                    imageName
                    lastStatusChange
                    machineId
                    memoryInGb
                    name
                    podType
                    port
                    ports
                    uptimeSeconds
                    vcpuCount
                    volumeInGb
                    volumeMountPath
                    runtime {
                        ports {
                            ip
                            isIpPublic
                            privatePort
                            publicPort
                            type
                        }
                    }
                    machine {
                        gpuDisplayName
                    }
                }
            }
        }
    ";


    /// <summary>
    /// GraphQL query to fetch all available GPU types with basic information.
    /// </summary>
    public static readonly string GpuTypes = @"
        query GpuTypes {
          gpuTypes {
            id
            displayName
            memoryInGb
          }
        }
    ";


    /// <summary>
    /// GraphQL query to fetch all serverless endpoints owned by the current user.
    /// </summary>
    public static readonly string Endpoint = @"
        query Query {
          myself {
            endpoints {
              aiKey
              gpuIds
              id
              idleTimeout
              name
              networkVolumeId
              locations
              scalerType
              scalerValue
              templateId
              type
              userId
              version
              workersMax
              workersMin
              workersStandby
              gpuCount
              env {
                key
                value
              }
              createdAt
              networkVolume {
                id
                dataCenterId
              }
            }
          }
        }
    ";


    /// <summary>
    /// GraphQL query to fetch current user information including network volumes and SSH keys.
    /// </summary>
    public static readonly string User = @"
        query myself {
            myself {
                id
                pubKey
                networkVolumes {
                    id
                    name
                    size
                    dataCenterId
                }
            }
        }
    ";



    /// <summary>
    /// Generates a GraphQL query to fetch detailed information about a specific GPU type.
    /// </summary>
    /// <param name="gpuId">GPU type identifier (e.g., "NVIDIA RTX A6000").</param>
    /// <param name="gpuCount">Number of GPUs to query pricing for. Default: 1.</param>
    /// <returns>GraphQL query string.</returns>
    public static string GenerateGpuQuery(string gpuId, int gpuCount = 1) {
        return $@"
            query GpuTypes {{
              gpuTypes(input: {{id: ""{gpuId}""}}) {{
                maxGpuCount
                id
                displayName
                manufacturer
                memoryInGb
                cudaCores
                secureCloud
                communityCloud
                securePrice
                communityPrice
                oneMonthPrice
                threeMonthPrice
                oneWeekPrice
                communitySpotPrice
                secureSpotPrice
                lowestPrice(input: {{gpuCount: {gpuCount}}}) {{
                  minimumBidPrice
                  uninterruptablePrice
                }}
              }}
            }}
        ";
    }



    /// <summary>
    /// Generates a GraphQL query to fetch detailed information about a specific pod.
    /// </summary>
    /// <param name="podId">Pod identifier.</param>
    /// <returns>GraphQL query string.</returns>
    public static string GeneratePodQuery(string podId) {
        return $@"
        query pod {{
            pod(input: {{podId: ""{podId}""}}) {{
                id
                containerDiskInGb
                costPerHr
                desiredStatus
                dockerArgs
                dockerId
                env
                gpuCount
                imageName
                lastStatusChange
                machineId
                memoryInGb
                name
                podType
                port
                ports
                uptimeSeconds
                vcpuCount
                volumeInGb
                volumeMountPath
                runtime {{
                    ports {{
                        ip
                        isIpPublic
                        privatePort
                        publicPort
                        type
                    }}
                }}
                machine {{
                    gpuDisplayName
                }}
            }}
        }}
        ";
    }
}
