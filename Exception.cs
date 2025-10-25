namespace Runpod.SDK;


/// <summary>
/// Exception thrown when a RunPod job fails, times out, or is cancelled.
/// </summary>
public class JobErrorException : Exception {
    /// <summary>
    /// The job status that triggered this exception (FAILED, TIMED_OUT, or CANCELLED).
    /// </summary>
    public readonly string status;

    /// <summary>
    /// Initializes a new instance of JobErrorException.
    /// </summary>
    /// <param name="status">The job status (FAILED, TIMED_OUT, or CANCELLED).</param>
    /// <param name="message">Optional error message from the job.</param>
    public JobErrorException(string status, string? message = null) : base(message) {
        this.status = status;
    }
}


/// <summary>
/// Exception thrown when API authentication fails due to invalid or missing API key.
/// </summary>
public class AuthenticationException : Exception {
    /// <summary>
    /// Initializes a new instance of AuthenticationException.
    /// </summary>
    /// <param name="message">Optional error message.</param>
    public AuthenticationException(string? message = null) : base(message) { }
}


/// <summary>
/// Exception thrown when a GraphQL query or mutation fails.
/// </summary>
public class QueryException : Exception {
    /// <summary>
    /// The GraphQL query or mutation that failed.
    /// </summary>
    public readonly string? query;

    /// <summary>
    /// Initializes a new instance of QueryException.
    /// </summary>
    /// <param name="message">Error message from GraphQL response.</param>
    /// <param name="query">The query or mutation that failed.</param>
    public QueryException(string? message = null, string? query = null) : base(message) {
        this.query = query;
    }
}
