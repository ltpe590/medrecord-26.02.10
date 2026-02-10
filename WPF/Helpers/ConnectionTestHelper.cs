using System;
using System.Net.NetworkInformation;
using System.Net;
using System.Windows;

namespace WPF.Helpers
{
    public static class ConnectionTestHelper
    {
        public static void TestApiConnection(string apiUrl, Action<string>? statusCallback = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiUrl))
                {
                    MessageBox.Show("Please enter an API URL first.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate URL format first
                if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uri))
                {
                    MessageBox.Show("Please enter a valid URL format.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                statusCallback?.Invoke($"Testing connection to {uri.Host}...");

                // Test connection using ping to the host
                var ping = new Ping();
                var reply = ping.Send(uri.Host, 3000); // 3 second timeout

                if (reply.Status == IPStatus.Success)
                {
                    statusCallback?.Invoke($"Connection to {uri.Host} successful ✓");
                    MessageBox.Show($"Connection to {uri.Host} successful! ✓", "Connection Test",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    statusCallback?.Invoke($"Connection to {uri.Host} failed ✘");
                    MessageBox.Show($"Connection to {uri.Host} failed: {reply.Status}", "Connection Test",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                statusCallback?.Invoke($"Test failed: {ex.Message}");
                MessageBox.Show($"Connection test failed: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
