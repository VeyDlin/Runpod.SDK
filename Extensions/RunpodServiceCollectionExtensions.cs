#if NET6_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Runpod.SDK.Client;

namespace Runpod.SDK.Extensions;


/// <summary>
/// Extension methods for registering RunPod services with dependency injection.
/// </summary>
public static class RunpodServiceCollectionExtensions {
    /// <summary>
    /// Adds RunPod client factory to the service collection.
    /// Use this for multi-tenant scenarios where API keys are retrieved dynamically (e.g., from database).
    /// All clients created by the factory share the same HttpClient pool for optimal performance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action for RunpodClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Program.cs
    /// builder.Services.AddRunpodFactory(options => {
    ///     options.GraphqlAddress = "https://api.runpod.io";
    ///     options.EndpointAddress = "https://api.runpod.ai";
    /// });
    ///
    /// // Controller
    /// public class ImageController : ControllerBase {
    ///     private readonly IRunpodClientFactory _factory;
    ///     private readonly AppDbContext _db;
    ///
    ///     public ImageController(IRunpodClientFactory factory, AppDbContext db) {
    ///         _factory = factory;
    ///         _db = db;
    ///     }
    ///
    ///     [HttpPost("generate")]
    ///     public async Task&lt;IActionResult&gt; Generate() {
    ///         var userApiKey = await _db.GetUserApiKey(User.Id);
    ///         using var runpod = _factory.CreateClient(userApiKey);
    ///         var result = await runpod.Endpoint("endpoint-id").RunSync&lt;Result&gt;(input);
    ///         return Ok(result);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddRunpodFactory(
        this IServiceCollection services,
        Action<RunpodClientOptions>? configure = null
    ) {
        var options = new RunpodClientOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register named HttpClient for IHttpClientFactory
        services.AddHttpClient(
            RunpodClientFactory.GetHttpClientName(),
            client => {
                client.BaseAddress = new Uri(options.endpointAddress);
                client.Timeout = TimeSpan.FromMinutes(10);
            }
        );

        services.AddSingleton<IRunpodClientFactory, RunpodClientFactory>();

        return services;
    }


    /// <summary>
    /// Adds RunPod client as singleton with a single API key.
    /// Use this for simple scenarios where the entire application uses one API key.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">RunPod API key. Get yours at https://www.runpod.io/console/user/settings</param>
    /// <param name="configure">Optional configuration action for RunpodClientOptions.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // Program.cs
    /// builder.Services.AddRunpod(
    ///     builder.Configuration["RunPod:ApiKey"]!,
    ///     options => {
    ///         options.EnableRateLimiting = true;
    ///     }
    /// );
    ///
    /// // Controller
    /// public class ImageController : ControllerBase {
    ///     private readonly IRunpodClient _runpod;
    ///
    ///     public ImageController(IRunpodClient runpod) {
    ///         _runpod = runpod;
    ///     }
    ///
    ///     [HttpPost("generate")]
    ///     public async Task&lt;IActionResult&gt; Generate([FromBody] Request req) {
    ///         var endpoint = _runpod.Endpoint("endpoint-id");
    ///         var result = await endpoint.RunSync&lt;ImageResult&gt;(req);
    ///         return Ok(result);
    ///     }
    /// }
    /// </code>
    /// </example>
    public static IServiceCollection AddRunpod(
        this IServiceCollection services,
        string apiKey,
        Action<RunpodClientOptions>? configure = null
    ) {
        if (string.IsNullOrWhiteSpace(apiKey)) {
            throw new ArgumentNullException(
                nameof(apiKey),
                "RunPod API key is required. Get yours at https://www.runpod.io/console/user/settings"
            );
        }

        var options = new RunpodClientOptions { apiKey = apiKey };
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IRunpodClient>(sp => {
            var opts = sp.GetRequiredService<RunpodClientOptions>();
            return new RunpodClient(
                opts.apiKey!,
                new RunpodHttpClient(opts.endpointAddress, opts.graphqlAddress)
            );
        });

        return services;
    }
}
#endif
