using System.Net;

namespace Core.Exceptions
{
    public abstract class DomainException : Exception
    {
        public ErrorSeverity Severity { get; protected set; } = ErrorSeverity.Medium;
        public string ErrorCode { get; protected set; } = "DOMAIN_ERROR";
        public string? UserMessage { get; protected set; }

        protected DomainException(string message) : base(message)
        {
            UserMessage = message;
        }

        protected DomainException(string message, string userMessage) : base(message)
        {
            UserMessage = userMessage;
        }

        protected DomainException(string message, string userMessage, Exception innerException)
            : base(message, innerException)
        {
            UserMessage = userMessage;
        }

        protected DomainException(string message, Exception innerException)
            : base(message, innerException)
        {
            UserMessage = message;
        }
    }

    // ============ Specific Domain Exceptions ============

    public class PatientNotFoundException : DomainException
    {
        public int PatientId { get; }

        public PatientNotFoundException(int patientId)
            : base($"Patient with ID {patientId} not found",
                  "The patient could not be found. Please check the patient ID and try again.")
        {
            PatientId = patientId;
            Severity = ErrorSeverity.High;
            ErrorCode = "PATIENT_NOT_FOUND";
        }
    }

    public class VisitNotFoundException : DomainException
    {
        public int VisitId { get; }

        public VisitNotFoundException(int visitId)
            : base($"Visit with ID {visitId} not found",
                  "The visit record could not be found.")
        {
            VisitId = visitId;
            Severity = ErrorSeverity.High;
            ErrorCode = "VISIT_NOT_FOUND";
        }
    }

    public class ValidationException : DomainException
    {
        public Dictionary<string, string[]> Errors { get; }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("One or more validation errors occurred",
                  "Please check your input and try again.")
        {
            Errors = errors;
            Severity = ErrorSeverity.Medium;
            ErrorCode = "VALIDATION_ERROR";
        }
    }

    public class AuthenticationException : DomainException
    {
        public AuthenticationException(string message)
            : base(message, "Invalid username or password. Please try again.")
        {
            Severity = ErrorSeverity.High;
            ErrorCode = "AUTHENTICATION_FAILED";
        }
    }

    public class AuthorizationException : DomainException
    {
        public string RequiredPermission { get; }

        public AuthorizationException(string requiredPermission)
            : base($"Permission denied: {requiredPermission}",
                  "You do not have permission to perform this action.")
        {
            RequiredPermission = requiredPermission;
            Severity = ErrorSeverity.High;
            ErrorCode = "AUTHORIZATION_DENIED";
        }
    }

    public class BusinessRuleException : DomainException
    {
        public string RuleName { get; }

        public BusinessRuleException(string ruleName, string message)
            : base($"Business rule violation: {ruleName} - {message}", message)
        {
            RuleName = ruleName;
            Severity = ErrorSeverity.High;
            ErrorCode = "BUSINESS_RULE_VIOLATION";
        }
    }

    public class DuplicateRecordException : DomainException
    {
        public string EntityType { get; }
        public string Identifier { get; }

        public DuplicateRecordException(string entityType, string identifier)
            : base($"{entityType} with identifier '{identifier}' already exists",
                  $"This {entityType.ToLower()} already exists in the system.")
        {
            EntityType = entityType;
            Identifier = identifier;
            Severity = ErrorSeverity.Medium;
            ErrorCode = "DUPLICATE_RECORD";
        }
    }

    // ============ Infrastructure/External Service Exceptions ============

    public class ApiConnectionException : DomainException
    {
        public string ApiUrl { get; }

        public ApiConnectionException(string apiUrl, string message)
            : base($"Cannot connect to API at {apiUrl}: {message}",
                  "Unable to connect to the server. Please check your network connection and try again.")
        {
            ApiUrl = apiUrl;
            Severity = ErrorSeverity.Critical;
            ErrorCode = "API_CONNECTION_FAILED";
        }
    }

    public class DatabaseException : DomainException
    {
        public string Operation { get; }

        public DatabaseException(string operation, Exception innerException)
            : base($"Database error during {operation}: {innerException.Message}",
                  "A database error occurred. Please try again or contact support.",
                  innerException)  // Now this works
        {
            Operation = operation;
            Severity = ErrorSeverity.Critical;
            ErrorCode = "DATABASE_ERROR";
        }
    }

    public class ExternalServiceException : DomainException
    {
        public string ServiceName { get; }
        public HttpStatusCode? StatusCode { get; }

        public ExternalServiceException(string serviceName, string message, HttpStatusCode? statusCode = null)
            : base($"{serviceName} error: {message}",
                  "An external service is temporarily unavailable. Please try again later.")
        {
            ServiceName = serviceName;
            StatusCode = statusCode;
            Severity = ErrorSeverity.High;
            ErrorCode = "EXTERNAL_SERVICE_ERROR";
        }
    }

    // ============ Shared Enums ============

    public enum ErrorSeverity
    {
        /// <summary>Informational, no action required</summary>
        Low = 1,

        /// <summary>Warning, may need attention</summary>
        Medium = 2,

        /// <summary>Error, action required</summary>
        High = 3,

        /// <summary>Critical, immediate action required</summary>
        Critical = 4
    }

    public enum ErrorCategory
    {
        Validation,
        Authentication,
        Authorization,
        BusinessLogic,
        Database,
        Network,
        ExternalService,
        System
    }
}