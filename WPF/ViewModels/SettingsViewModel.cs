using Core.Interfaces.Services;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPF.Configuration;

namespace WPF.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IAppSettingsService _appSettings;
        private readonly IConnectionService _connectionService;
        private readonly ILogger<SettingsViewModel> _logger;
        private string _apiBaseUrl = string.Empty;
        private string? _defaultUser;
        private string? _defaultPassword;
        private string? _defaultUserName;
        private bool _enableDetailedErrors;
        private int _httpTimeoutSeconds = 30;
        private bool _testConnectionAfterSave = true;

        // Connection testing properties
        private bool _isTestingConnection;

        private string _connectionStatus = "Not tested";
        private string _connectionStatusColor = "Gray";

        public SettingsViewModel(
            IAppSettingsService appSettings,
            IConnectionService connectionService,
            ILogger<SettingsViewModel> logger)

        {
            _appSettings = appSettings;
            _connectionService = connectionService;
            _logger = logger;

            LoadCurrentSettings();

            // Subscribe to connection status changes
            _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;

            _logger.LogInformation("SettingsViewModel initialized");
        }

        public string ApiBaseUrl
        {
            get => _apiBaseUrl;
            set
            {
                _apiBaseUrl = value;
                OnPropertyChanged();
            }
        }

        public string? DefaultUser
        {
            get => _defaultUser;
            set { _defaultUser = value; OnPropertyChanged(); }
        }

        public string? DefaultPassword
        {
            get => _defaultPassword;
            set { _defaultPassword = value; OnPropertyChanged(); }
        }

        public string? DefaultUserName
        {
            get => _defaultUserName;
            set { _defaultUserName = value; OnPropertyChanged(); }
        }

        public bool EnableDetailedErrors
        {
            get => _enableDetailedErrors;
            set { _enableDetailedErrors = value; OnPropertyChanged(); }
        }

        public int HttpTimeoutSeconds
        {
            get => _httpTimeoutSeconds;
            set { _httpTimeoutSeconds = value; OnPropertyChanged(); }
        }

        #region TestConnection

        public bool TestConnectionAfterSave
        {
            get => _testConnectionAfterSave;
            set { _testConnectionAfterSave = value; OnPropertyChanged(); }
        }

        // Connection testing properties
        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            private set
            {
                if (_isTestingConnection != value)
                {
                    _isTestingConnection = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanTestConnection));
                }
            }
        }

        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ConnectionStatusColor
        {
            get => _connectionStatusColor;
            private set
            {
                if (_connectionStatusColor != value)
                {
                    _connectionStatusColor = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool CanTestConnection =>
            !string.IsNullOrWhiteSpace(ApiBaseUrl) && !IsTestingConnection;

        public async Task TestConnectionAsync()
        {
            if (!CanTestConnection) return;

            IsTestingConnection = true;
            ConnectionStatus = "Testing connection...";
            ConnectionStatusColor = "Yellow";

            _logger.LogInformation("Testing API connection to: {ApiUrl}", ApiBaseUrl);

            try
            {
                // Call ConnectionService (testing logic lives here)
                var result = await _connectionService.TestApiConnectionAsync(ApiBaseUrl);

                // Update UI state based on result
                if (result.Success)
                {
                    ConnectionStatus = "✓ Connection successful!";
                    ConnectionStatusColor = "Green";

                    _logger.LogInformation("Connection test successful for {ApiUrl}", ApiBaseUrl);

                    // Show success message
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Successfully connected to:\n{ApiBaseUrl}\n\nResponse time: {result.ResponseTime.TotalMilliseconds:F0}ms",
                            "Connection Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
                else
                {
                    ConnectionStatus = $"✗ {result.Message}";
                    ConnectionStatusColor = "Red";

                    _logger.LogWarning("Connection test failed for {ApiUrl}: {ErrorMessage}",
                        ApiBaseUrl, result.Message);

                    // Show error message
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Connection failed:\n{result.Message}\n\nURL: {ApiBaseUrl}",
                            "Connection Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection to: {ApiUrl}", ApiBaseUrl);

                ConnectionStatus = "✗ Connection test error";
                ConnectionStatusColor = "Red";

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        $"Error testing connection: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        // Optional: Test connection after saving
        public async Task<bool> TestConnectionAfterSaveAsync()
        {
            if (!TestConnectionAfterSave) return true;

            try
            {
                _logger.LogInformation("Testing connection after save...");
                var result = await _connectionService.TestApiConnectionAsync(ApiBaseUrl);

                if (!result.Success)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"Settings saved but connection test failed:\n{result.Message}",
                            "Connection Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection after save");
                return false;
            }
        }

        private void OnConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            // Update from service layer
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.IsConnected)
                {
                    ConnectionStatus = "✓ Connected";
                    ConnectionStatusColor = "Green";
                }
                else
                {
                    ConnectionStatus = "✗ Disconnected";
                    ConnectionStatusColor = "Red";
                }
            });
        }

        // Clean up event subscription
        public void Cleanup()
        {
            _connectionService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        #endregion TestConnection

        private void LoadCurrentSettings()
        {
            ApiBaseUrl = _appSettings.ApiBaseUrl ?? "http://localhost:5258";
            DefaultUser = !string.IsNullOrEmpty(_appSettings.DefaultUser) ? _appSettings.DefaultUser : UserSettingsManager.GetDefaultUsername();
            DefaultPassword = !string.IsNullOrEmpty(_appSettings.DefaultPassword) ? _appSettings.DefaultPassword : UserSettingsManager.GetDefaultPassword();
            DefaultUserName = _appSettings.DefaultUserName ?? "Developer";
            EnableDetailedErrors = _appSettings.EnableDetailedErrors;
            HttpTimeoutSeconds = (int)_appSettings.HttpTimeout.TotalSeconds;
        }

        public void SaveSettings()
        {
            try
            {
                if (_appSettings is Core.Configuration.AppSettings concreteSettings)
                {
                    concreteSettings.ApiBaseUrl = ApiBaseUrl;
                    concreteSettings.DefaultUser = DefaultUser;
                    concreteSettings.DefaultPassword = DefaultPassword;
                    concreteSettings.DefaultUserName = DefaultUserName;
                    concreteSettings.EnableDetailedErrors = EnableDetailedErrors;
                    concreteSettings.HttpTimeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);

                    _logger.LogInformation("Application settings updated");
                }
                else
                {
                    _logger.LogWarning(
                        "AppSettings is not concrete AppSettings. Type: {Type}",
                        _appSettings.GetType().Name);

                    // Minimal fallback
                    SetPropertySafely("ApiBaseUrl", ApiBaseUrl);
                    SetPropertySafely("DefaultUser", DefaultUser);
                    SetPropertySafely("DefaultPassword", DefaultPassword);
                    SetPropertySafely("DefaultUserName", DefaultUserName);
                    SetPropertySafely("EnableDetailedErrors", EnableDetailedErrors);

                    var timeoutProp = _appSettings.GetType().GetProperty("HttpTimeout");
                    timeoutProp?.SetValue(_appSettings, TimeSpan.FromSeconds(HttpTimeoutSeconds));
                }

                UserSettingsManager.SaveCredentials(DefaultUser ?? string.Empty, DefaultPassword ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while saving settings");
                throw;
            }
        }

        private void SetPropertySafely(string propertyName, object? value)
        {
            var prop = _appSettings.GetType().GetProperty(propertyName);
            if (prop?.CanWrite == true)
            {
                prop.SetValue(_appSettings, value);
            }
        }

        public void CancelSettings()
        {
            // Reset to current settings
            LoadCurrentSettings();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}