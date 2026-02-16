using System.Net;

namespace Core.Exceptions
{
    public class ApiException : Exception
    {
        public HttpStatusCode? StatusCode { get; init; }
        public string? Endpoint { get; init; }
        public string? Method { get; init; }
        public string? ResponseContent { get; init; }
        public ErrorSeverity Severity { get; init; } = ErrorSeverity.High;
        public ErrorCategory Category { get; init; } = ErrorCategory.Network;

        public ApiException(string message) : base(message)
        {
        }

        public ApiException(string message, Exception innerException)
            : base(message, innerException) { }

        public ApiException(string message, HttpStatusCode statusCode, string endpoint, string method)
            : base(message)
        {
            StatusCode = statusCode;
            Endpoint = endpoint;
            Method = method;

            // Set severity based on status code
            Severity = statusCode switch
            {
                HttpStatusCode.BadRequest => ErrorSeverity.Medium,          // 400
                HttpStatusCode.Unauthorized => ErrorSeverity.High,          // 401
                HttpStatusCode.Forbidden => ErrorSeverity.High,            // 403
                HttpStatusCode.NotFound => ErrorSeverity.Medium,           // 404
                HttpStatusCode.Conflict => ErrorSeverity.High,             // 409
                HttpStatusCode.InternalServerError => ErrorSeverity.Critical, // 500
                HttpStatusCode.ServiceUnavailable => ErrorSeverity.Critical, // 503
                _ => ErrorSeverity.High
            };
        }

        public override string ToString()
        {
            return $"""
                ApiException: {Message}
                Category: {Category}
                Severity: {Severity}
                Endpoint: {Endpoint}
                Method: {Method}
                StatusCode: {StatusCode}
                Response: {ResponseContent}
                Stack Trace: {StackTrace}
                """;
        }
    }
}