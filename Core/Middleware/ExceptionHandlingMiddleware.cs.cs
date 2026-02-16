using Core.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Core.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");

            var response = context.Response;
            response.ContentType = "application/json";

            var (statusCode, result) = GetErrorResponse(exception);

            response.StatusCode = (int)statusCode;
            await response.WriteAsync(JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            }));
        }

        private (HttpStatusCode StatusCode, object Result) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                // FluentValidation exceptions
                FluentValidation.ValidationException fluentEx => (HttpStatusCode.BadRequest, CreateFluentValidationErrorResult(fluentEx)),

                // Your custom validation exceptions
                Core.Exceptions.ValidationException ex => (HttpStatusCode.BadRequest, CreateValidationErrorResult(ex)),

                // Domain exceptions
                PatientNotFoundException ex => (HttpStatusCode.NotFound, CreateDomainErrorResult(ex)),
                VisitNotFoundException ex => (HttpStatusCode.NotFound, CreateDomainErrorResult(ex)),
                AuthenticationException ex => (HttpStatusCode.Unauthorized, CreateDomainErrorResult(ex)),
                AuthorizationException ex => (HttpStatusCode.Forbidden, CreateDomainErrorResult(ex)),
                DuplicateRecordException ex => (HttpStatusCode.Conflict, CreateDomainErrorResult(ex)),
                BusinessRuleException ex => (HttpStatusCode.UnprocessableEntity, CreateDomainErrorResult(ex)),

                // API exceptions
                ApiException ex => (ex.StatusCode ?? HttpStatusCode.InternalServerError, CreateApiErrorResult(ex)),

                // Built-in exceptions
                KeyNotFoundException => (HttpStatusCode.NotFound, CreateSimpleError("Resource not found")),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, CreateSimpleError("Unauthorized")),

                // Default
                _ => (HttpStatusCode.InternalServerError, CreateSimpleError("An internal server error occurred"))
            };
        }

        private object CreateFluentValidationErrorResult(FluentValidation.ValidationException ex)
        {
            // Transform FluentValidation errors into your format
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return new
            {
                Error = "VALIDATION_ERROR",
                Message = "One or more validation errors occurred",
                Severity = "Medium",
                Errors = errors,
                Timestamp = DateTime.UtcNow
            };
        }

        private object CreateValidationErrorResult(Core.Exceptions.ValidationException ex)
        {
            return new
            {
                Error = ex.ErrorCode,
                Message = ex.UserMessage ?? ex.Message,
                Severity = ex.Severity.ToString(),
                Errors = ex.Errors,
                Timestamp = DateTime.UtcNow
            };
        }

        private object CreateDomainErrorResult(DomainException ex)
        {
            return new
            {
                Error = ex.ErrorCode,
                Message = ex.UserMessage ?? ex.Message,
                Severity = ex.Severity.ToString(),
                Timestamp = DateTime.UtcNow
            };
        }

        private object CreateApiErrorResult(ApiException ex)
        {
            return new
            {
                Error = "API_ERROR",
                Message = ex.Message,
                Severity = ex.Severity.ToString(),
                Category = ex.Category.ToString(),
                Endpoint = ex.Endpoint,
                Method = ex.Method,
                Timestamp = DateTime.UtcNow
            };
        }

        private object CreateSimpleError(string message)
        {
            return new
            {
                Error = "SERVER_ERROR",
                Message = message,
                Timestamp = DateTime.UtcNow
            };
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}