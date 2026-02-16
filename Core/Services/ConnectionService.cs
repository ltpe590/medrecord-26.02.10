using Core.Interfaces.Services;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Core.Services
{
    public class ConnectionService : IConnectionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ConnectionService> _logger;
        private bool _isApiConnected;

        public bool IsApiConnected => _isApiConnected;

        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        public ConnectionService(HttpClient httpClient, ILogger<ConnectionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<ConnectionTestResult> TestApiConnectionAsync(string apiUrl)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Testing API connection to: {ApiUrl}", apiUrl);

                // Validate URL
                if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                {
                    throw new ArgumentException("Invalid API URL format");
                }

                // Simple health check endpoint
                var healthEndpoint = $"{apiUrl.TrimEnd('/')}/health";

                var response = await _httpClient.GetAsync(healthEndpoint);
                _isApiConnected = response.IsSuccessStatusCode;

                var result = new ConnectionTestResult
                {
                    Success = _isApiConnected,
                    Message = _isApiConnected
                        ? "API connection successful"
                        : $"API returned status: {response.StatusCode}",
                    StatusCode = (int)response.StatusCode,
                    ResponseTime = stopwatch.Elapsed,
                    TestedAt = DateTime.UtcNow,
                    Endpoint = apiUrl
                };

                if (result.Success)
                {
                    _logger.LogInformation("API connection successful to {ApiUrl} in {ResponseTime}ms",
                        apiUrl, result.ResponseTime.TotalMilliseconds);
                }
                else
                {
                    _logger.LogWarning("API connection failed to {ApiUrl}: {ErrorMessage}",
                        apiUrl, result.Message);
                }

                OnConnectionStatusChanged(_isApiConnected, result.Message);
                return result;
            }
            catch (Exception ex)
            {
                _isApiConnected = false;

                var result = new ConnectionTestResult
                {
                    Success = false,
                    Message = $"Connection failed: {ex.Message}",
                    ResponseTime = stopwatch.Elapsed,
                    TestedAt = DateTime.UtcNow,
                    Endpoint = apiUrl,
                    Exception = ex
                };

                _logger.LogError(ex, "Exception testing API connection to: {ApiUrl}", apiUrl);
                OnConnectionStatusChanged(false, result.Message);
                return result;
            }
        }

        private void OnConnectionStatusChanged(bool isConnected, string message)
        {
            ConnectionStatusChanged?.Invoke(this,
                new ConnectionStatusChangedEventArgs(isConnected, message));
        }
    }
}