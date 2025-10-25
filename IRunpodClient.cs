using Runpod.SDK.API;
using Runpod.SDK.Endpoints;

namespace Runpod.SDK;


/// <summary>
/// Interface for RunPod API client.
/// </summary>
public interface IRunpodClient : IDisposable {
    /// <summary>
    /// GraphQL commands for pod and GPU management.
    /// </summary>
    Commands cmd { get; }

    /// <summary>
    /// Creates an endpoint client for serverless operations.
    /// </summary>
    /// <param name="id">The endpoint ID.</param>
    /// <returns>An endpoint client instance.</returns>
    Endpoint Endpoint(string id);
}
