namespace Runpod.SDK;


public class JobErrorException : Exception {
    public readonly string status;

    public JobErrorException(string status, string ? message = null) : base(message) {
        this.status = status;
    }
}


public class AuthenticationException : Exception {
    public AuthenticationException(string? message = null) : base(message) { }
}


public class QueryException : Exception {
    public readonly string? query;

    public QueryException(string? message = null, string? query = null) : base(message) {
        this.query = query;
    }
}
