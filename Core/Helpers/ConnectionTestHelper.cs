using Microsoft.Extensions.Logging;
using System.Net.NetworkInformation;

namespace Core.Helpers
{
    public static class ConnectionTestHelper
    {
        private static ILogger? _logger;

        public static void Initialize(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static void TestApiConnection(string apiUrl, Action<string>? statusCallback = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    LogWarning("Connection test failed: Please enter an API URL first.");
                    statusCallback?.Invoke("Please enter an API URL first.");
                    return;
                }

                // Validate URL format first
                if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                {
                    LogWarning("Connection test failed: Invalid URL format.");
                    statusCallback?.Invoke("Please enter a valid URL format.");
                    return;
                }

                LogInformation($"Starting connection test to {uri.Host}");
                statusCallback?.Invoke($"Testing connection to {uri.Host}...");

                // Test connection using ping to the host
                var ping = new Ping();
                var reply = ping.Send(uri.Host, 3000); // 3 second timeout

                if (reply.Status == IPStatus.Success)
                {
                    LogInformation($"Connection to {uri.Host} successful");
                    statusCallback?.Invoke($"Connection to {uri.Host} successful ✓");
                }
                else
                {
                    LogWarning($"Connection to {uri.Host} failed: {reply.Status}");
                    statusCallback?.Invoke($"Connection to {uri.Host} failed ✘");
                }
            }
            catch (PingException ex)
            {
                LogError($"Ping test failed for {apiUrl}", ex);
                statusCallback?.Invoke($"Ping test failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"Connection test failed for {apiUrl}", ex);
                statusCallback?.Invoke($"Test failed: {ex.Message}");
            }
        }

        #region Private Helper Methods

        private static void LogInformation(string message)
        {
            _logger?.LogInformation(message);
        }

        private static void LogWarning(string message)
        {
            _logger?.LogWarning(message);
        }

        private static void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger?.LogError(exception, message);
            }
            else
            {
                _logger?.LogError(message);
            }
        }

        #endregion Private Helper Methods
    }
}