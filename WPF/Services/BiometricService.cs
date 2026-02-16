using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Windows.Security.Credentials.UI;

namespace WPF.Services
{
    public interface IBiometricService
    {
        Task<bool> IsAvailableAsync();
        Task<(bool Success, string Message)> AuthenticateAsync(string username);
    }

    public class BiometricService : IBiometricService
    {
        private readonly ILogger<BiometricService> _logger;

        public BiometricService(ILogger<BiometricService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                _logger.LogInformation("=== Checking Biometric Availability ===");
                
                var availability = await UserConsentVerifier.CheckAvailabilityAsync();
                
                _logger.LogInformation("   Biometric Status: {Status}", availability);
                
                var isAvailable = availability == UserConsentVerifierAvailability.Available;
                
                if (isAvailable)
                {
                    _logger.LogInformation("✅ Biometric authentication is AVAILABLE");
                }
                else
                {
                    _logger.LogWarning("❌ Biometric authentication is NOT available: {Reason}", availability);
                }
                
                return isAvailable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking biometric availability");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> AuthenticateAsync(string username)
        {
            try
            {
                _logger.LogInformation("=== Starting Biometric Authentication ===");
                _logger.LogInformation("   User: {Username}", username);
                
                // Check availability first
                var isAvailable = await IsAvailableAsync();
                if (!isAvailable)
                {
                    _logger.LogWarning("❌ Biometric not available, cannot authenticate");
                    return (false, "Biometric authentication not available on this device");
                }

                _logger.LogInformation("⏳ Requesting biometric verification...");
                
                var result = await UserConsentVerifier.RequestVerificationAsync(
                    $"Verify identity for {username}");

                _logger.LogInformation("   Verification Result: {Result}", result);

                switch (result)
                {
                    case UserConsentVerificationResult.Verified:
                        _logger.LogInformation("✅ Biometric authentication SUCCESSFUL");
                        return (true, "Authentication successful");

                    case UserConsentVerificationResult.Canceled:
                        _logger.LogWarning("⚠️ User CANCELED biometric authentication");
                        return (false, "Authentication canceled by user");

                    case UserConsentVerificationResult.DeviceBusy:
                        _logger.LogWarning("⚠️ Device is BUSY");
                        return (false, "Device is busy, please try again");

                    case UserConsentVerificationResult.NotConfiguredForUser:
                        _logger.LogWarning("⚠️ Biometric NOT configured for this user");
                        return (false, "Biometric authentication not set up for this user");

                    case UserConsentVerificationResult.DisabledByPolicy:
                        _logger.LogWarning("⚠️ Biometric DISABLED by policy");
                        return (false, "Biometric authentication disabled by policy");

                    case UserConsentVerificationResult.RetriesExhausted:
                        _logger.LogError("❌ Too many FAILED attempts");
                        return (false, "Too many failed attempts");

                    default:
                        _logger.LogError("❌ Unknown verification result: {Result}", result);
                        return (false, $"Authentication failed: {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during biometric authentication");
                return (false, $"Authentication error: {ex.Message}");
            }
        }
    }
}
