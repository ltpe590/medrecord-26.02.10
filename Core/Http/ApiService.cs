using Core.Exceptions;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Core.Http
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthSession _authSession;
        private readonly IAppSettingsService _appSettings;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ApiService> _logger;

        public ApiService(
            HttpClient httpClient,
            IAuthSession authSession,
            ILogger<ApiService> logger,
            IAppSettingsService appSettings)
        {
            _httpClient = httpClient;
            _authSession = authSession;
            _appSettings = appSettings;
            _logger = logger;

            _httpClient.Timeout = _appSettings.HttpTimeout;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MedRecords/1.0");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public void SetAuthToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
        {
            using var requestScope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "ApiMethod", "GET" },
                { "ApiEndpoint", endpoint }
            });

            var url = BuildUrl(endpoint);
            var request = CreateRequest(HttpMethod.Get, url);

            _logger.LogInformation("Sending API Request: {HttpMethod} {Url}", request.Method, url);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request, ct);
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;

                var responseContent = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Received API Response in {Duration}ms: {StatusCode} {ReasonPhrase}",
                                       durationMs, (int)response.StatusCode, response.ReasonPhrase);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response, endpoint, "GET");
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return default;
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("API request was cancelled for {HttpMethod} {Endpoint}", "GET", endpoint);
                throw new OperationCanceledException($"Request to {endpoint} was cancelled", ex, ct);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "API request timed out after {TimeoutSeconds}s for {HttpMethod} {Endpoint}",
                                   _httpClient.Timeout.TotalSeconds, "GET", endpoint);

                throw new ApiException($"Request to {endpoint} timed out after {_httpClient.Timeout.TotalSeconds}s", ex)
                {
                    Endpoint = endpoint,
                    Method = "GET"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException occurred for {HttpMethod} {Endpoint}", "GET", endpoint);
                throw new ApiException($"GET request to {endpoint} failed", ex)
                {
                    StatusCode = ex.StatusCode,
                    Endpoint = endpoint,
                    Method = "GET"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Deserialization failed for {HttpMethod} {Endpoint}", "GET", endpoint);
                throw new ApiException($"Failed to deserialize response from {endpoint}", ex)
                {
                    Endpoint = endpoint,
                    Method = "GET"
                };
            }
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            using var requestScope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "ApiMethod", "POST" },
                { "ApiEndpoint", endpoint }
            });

            var url = BuildUrl(endpoint);
            var request = CreateRequest(HttpMethod.Post, url);

            var requestJson = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending API Request with payload: {RequestJson}", requestJson);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request, ct);
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;

                var responseContent = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Received API Response in {Duration}ms: {StatusCode} {ReasonPhrase}",
                                       durationMs, (int)response.StatusCode, response.ReasonPhrase);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response, endpoint, "POST");
                }

                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return default!;
                }

                return JsonSerializer.Deserialize<TResponse>(responseContent, _jsonOptions)
                    ?? throw new InvalidOperationException($"Deserialization returned null for {endpoint}");
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("API request was cancelled for {HttpMethod} {Endpoint}", "POST", endpoint);
                throw new OperationCanceledException($"Request to {endpoint} was cancelled", ex, ct);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "API request timed out after {TimeoutSeconds}s for {HttpMethod} {Endpoint}",
                                   _httpClient.Timeout.TotalSeconds, "POST", endpoint);
                throw new ApiException($"Request to {endpoint} timed out after {_httpClient.Timeout.TotalSeconds}s", ex)
                {
                    Endpoint = endpoint,
                    Method = "POST"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException occurred for {HttpMethod} {Endpoint}", "POST", endpoint);
                throw new ApiException($"POST request to {endpoint} failed", ex)
                {
                    StatusCode = ex.StatusCode,
                    Endpoint = endpoint,
                    Method = "POST"
                };
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Deserialization failed for {HttpMethod} {Endpoint}", "POST", endpoint);
                throw new ApiException($"Failed to deserialize response from {endpoint}", ex)
                {
                    Endpoint = endpoint,
                    Method = "POST"
                };
            }
        }

        public async Task PutAsync<TRequest>(string endpoint, TRequest data, CancellationToken ct = default)
        {
            using var requestScope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "ApiMethod", "PUT" },
                { "ApiEndpoint", endpoint }
            });

            var url = BuildUrl(endpoint);
            var request = CreateRequest(HttpMethod.Put, url);

            var requestJson = JsonSerializer.Serialize(data, _jsonOptions);
            request.Content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending API Request with payload: {RequestJson}", requestJson);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request, ct);
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;

                var responseContent = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Received API Response in {Duration}ms: {StatusCode} {ReasonPhrase}",
                                       durationMs, (int)response.StatusCode, response.ReasonPhrase);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response, endpoint, "PUT");
                }
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("API request was cancelled for {HttpMethod} {Endpoint}", "PUT", endpoint);
                throw new OperationCanceledException($"Request to {endpoint} was cancelled", ex, ct);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "API request timed out after {TimeoutSeconds}s for {HttpMethod} {Endpoint}",
                                   _httpClient.Timeout.TotalSeconds, "PUT", endpoint);
                throw new ApiException($"Request to {endpoint} timed out after {_httpClient.Timeout.TotalSeconds}s", ex)
                {
                    Endpoint = endpoint,
                    Method = "PUT"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException occurred for {HttpMethod} {Endpoint}", "PUT", endpoint);
                throw new ApiException($"PUT request to {endpoint} failed", ex)
                {
                    StatusCode = ex.StatusCode,
                    Endpoint = endpoint,
                    Method = "PUT"
                };
            }
        }

        public async Task DeleteAsync(string endpoint, CancellationToken ct = default)
        {
            using var requestScope = _logger.BeginScope(new Dictionary<string, object>
            {
                { "ApiMethod", "DELETE" },
                { "ApiEndpoint", endpoint }
            });

            var url = BuildUrl(endpoint);
            var request = CreateRequest(HttpMethod.Delete, url);

            _logger.LogInformation("Sending API Request: {HttpMethod} {Url}", request.Method, url);

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await _httpClient.SendAsync(request, ct);
                stopwatch.Stop();
                var durationMs = stopwatch.ElapsedMilliseconds;

                var responseContent = await response.Content.ReadAsStringAsync(ct);

                _logger.LogInformation("Received API Response in {Duration}ms: {StatusCode} {ReasonPhrase}",
                                       durationMs, (int)response.StatusCode, response.ReasonPhrase);

                if (!response.IsSuccessStatusCode)
                {
                    await HandleErrorResponse(response, endpoint, "DELETE");
                }
            }
            catch (TaskCanceledException ex) when (ct.IsCancellationRequested)
            {
                _logger.LogInformation("API request was cancelled for {HttpMethod} {Endpoint}", "DELETE", endpoint);
                throw new OperationCanceledException($"Request to {endpoint} was cancelled", ex, ct);
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "API request timed out after {TimeoutSeconds}s for {HttpMethod} {Endpoint}",
                                   _httpClient.Timeout.TotalSeconds, "DELETE", endpoint);
                throw new ApiException($"Request to {endpoint} timed out after {_httpClient.Timeout.TotalSeconds}s", ex)
                {
                    Endpoint = endpoint,
                    Method = "DELETE"
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException occurred for {HttpMethod} {Endpoint}", "DELETE", endpoint);
                throw new ApiException($"DELETE request to {endpoint} failed", ex)
                {
                    StatusCode = ex.StatusCode,
                    Endpoint = endpoint,
                    Method = "DELETE"
                };
            }
        }

        #region Helper Methods

        private string BuildUrl(string endpoint)
        {
            if (Uri.IsWellFormedUriString(endpoint, UriKind.Absolute))
            {
                return endpoint;
            }

            var baseUrl = _appSettings.ApiBaseUrl?.TrimEnd('/') ?? "http://localhost:5258";
            var normalizedEndpoint = endpoint.TrimStart('/');

            return $"{baseUrl}/{normalizedEndpoint}";
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            if (_authSession.IsAuthenticated && !string.IsNullOrEmpty(_authSession.Token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer", _authSession.Token);
            }

            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            return request;
        }

        private async Task HandleErrorResponse(HttpResponseMessage response, string endpoint, string method)
        {
            var statusCode = (int)response.StatusCode;
            var reasonPhrase = response.ReasonPhrase ?? "Unknown";

            string errorDetails;
            try
            {
                errorDetails = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                errorDetails = "[Unable to read error details]";
            }

            // Try to parse error response
            string? errorMessage = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(errorDetails))
                {
                    var errorObject = JsonDocument.Parse(errorDetails);
                    if (errorObject.RootElement.TryGetProperty("message", out var messageElement))
                    {
                        errorMessage = messageElement.GetString();
                    }
                    else if (errorObject.RootElement.TryGetProperty("error", out var errorElement))
                    {
                        errorMessage = errorElement.GetString();
                    }
                }
            }
            catch
            {
                // If we can't parse as JSON, use the raw content
            }

            errorMessage ??= errorDetails;

            throw new ApiException($"HTTP {statusCode}: {reasonPhrase}. {errorMessage}")
            {
                StatusCode = response.StatusCode,
                Endpoint = endpoint,
                Method = method,
                ResponseContent = errorDetails
            };
        }

        #endregion Helper Methods
    }
}