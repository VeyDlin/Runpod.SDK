namespace Runpod.SDK.API;


internal static class Queries {
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


    public static readonly string GpuTypes = @"
        query GpuTypes {
          gpuTypes {
            id
            displayName
            memoryInGb
          }
        }
    ";


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
