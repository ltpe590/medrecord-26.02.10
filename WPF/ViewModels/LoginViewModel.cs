using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WPF.Services;

namespace WPF.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IUserService _userService;
        private readonly IBiometricService _biometricService;
        private readonly IAppSettingsService _settings;
        private readonly ILogger<LoginViewModel> _logger;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _statusMessage = "Please login";
        private bool _isLoggingIn = false;
        private bool _biometricAvailable = false;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? LoginSuccessful; // Passes auth token
        public event Action<string, string>? ShowError;

        public LoginViewModel(
            IUserService userService,
            IBiometricService biometricService,
            IAppSettingsService settings,
            ILogger<LoginViewModel> logger)
        {
            _userService = userService;
            _biometricService = biometricService;
            _settings = settings;
            _logger = logger;

            _ = CheckBiometricAvailabilityAsync();
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set
            {
                if (_isLoggingIn != value)
                {
                    _isLoggingIn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanLogin));
                }
            }
        }

        public bool BiometricAvailable
        {
            get => _biometricAvailable;
            set
            {
                if (_biometricAvailable != value)
                {
                    _biometricAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanLogin => !IsLoggingIn && !string.IsNullOrWhiteSpace(Username);

        private async Task CheckBiometricAvailabilityAsync()
        {
            try
            {
                _logger.LogInformation("=== LoginViewModel: Checking biometric availability ===");
                BiometricAvailable = await _biometricService.IsAvailableAsync();
                _logger.LogInformation("   Biometric Available: {Available}", BiometricAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking biometric availability");
                BiometricAvailable = false;
            }
        }

        public async Task<bool> LoginWithPasswordAsync()
        {
            _logger.LogInformation("=== LoginWithPasswordAsync CALLED ===");
            _logger.LogInformation("   Username: {Username}", Username);
            _logger.LogInformation("   Password Length: {Length}", Password?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(Username))
            {
                _logger.LogWarning("❌ Username is empty");
                ShowError?.Invoke("Validation Error", "Username is required");
                return false;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                _logger.LogWarning("❌ Password is empty");
                ShowError?.Invoke("Validation Error", "Password is required");
                return false;
            }

            IsLoggingIn = true;
            StatusMessage = "Logging in...";

            try
            {
                _logger.LogInformation("⏳ Calling UserService.LoginAsync...");
                
                var authToken = await _userService.LoginAsync(
                    Username, 
                    Password, 
                    _settings.ApiBaseUrl);

                _logger.LogInformation("   Login completed. Token: {HasToken}", !string.IsNullOrEmpty(authToken));

                if (!string.IsNullOrEmpty(authToken))
                {
                    _logger.LogInformation("✅ Login SUCCESSFUL");
                    _logger.LogInformation("   Token received (length: {Length})", authToken.Length);
                    
                    StatusMessage = "Login successful!";
                    LoginSuccessful?.Invoke(authToken);
                    return true;
                }
                else
                {
                    _logger.LogWarning("❌ Login FAILED: Empty token returned");
                    StatusMessage = "Login failed";
                    ShowError?.Invoke("Login Failed", "Invalid credentials");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during login");
                StatusMessage = "Login error";
                ShowError?.Invoke("Login Error", $"Error: {ex.Message}");
                return false;
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        public async Task<bool> LoginWithBiometricAsync()
        {
            _logger.LogInformation("=== LoginWithBiometricAsync CALLED ===");
            _logger.LogInformation("   Username: {Username}", Username);

            if (string.IsNullOrWhiteSpace(Username))
            {
                _logger.LogWarning("❌ Username is empty");
                ShowError?.Invoke("Validation Error", "Username is required for biometric login");
                return false;
            }

            if (!BiometricAvailable)
            {
                _logger.LogWarning("❌ Biometric not available");
                ShowError?.Invoke("Not Available", "Biometric authentication is not available");
                return false;
            }

            IsLoggingIn = true;
            StatusMessage = "Authenticating with biometric...";

            try
            {
                _logger.LogInformation("⏳ Requesting biometric authentication...");
                
                var (success, message) = await _biometricService.AuthenticateAsync(Username);

                _logger.LogInformation("   Biometric Result - Success: {Success}, Message: {Message}", 
                    success, message);

                if (success)
                {
                    _logger.LogInformation("✅ Biometric authentication SUCCESSFUL");
                    _logger.LogInformation("⏳ Now logging in with stored credentials...");
                    
                    // TODO: Retrieve stored password for this user
                    // For now, we'll require password to be entered
                    // In production, you'd store encrypted credentials after first login
                    
                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        _logger.LogWarning("⚠️ Password required for first-time biometric login");
                        StatusMessage = "Please enter password for first-time biometric setup";
                        ShowError?.Invoke("Password Required", 
                            "Please enter your password the first time, it will be stored securely for future biometric logins");
                        return false;
                    }

                    // Login with password
                    return await LoginWithPasswordAsync();
                }
                else
                {
                    _logger.LogWarning("❌ Biometric authentication FAILED: {Message}", message);
                    StatusMessage = "Biometric authentication failed";
                    ShowError?.Invoke("Authentication Failed", message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception during biometric login");
                StatusMessage = "Biometric login error";
                ShowError?.Invoke("Login Error", $"Error: {ex.Message}");
                return false;
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
