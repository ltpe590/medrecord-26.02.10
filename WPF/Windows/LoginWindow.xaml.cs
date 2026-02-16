using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Windows;
using WPF.ViewModels;

namespace WPF.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;
        private readonly ILogger<LoginWindow> _logger;

        public string? AuthToken { get; private set; }
        public bool LoginSuccess { get; private set; }

        public LoginWindow()
        {
            InitializeComponent();

            _logger = App.Services.GetRequiredService<ILogger<LoginWindow>>();
            _viewModel = App.Services.GetRequiredService<LoginViewModel>();

            DataContext = _viewModel;

            _logger.LogInformation("=== LoginWindow CREATED ===");
            Debug.WriteLine("=== LoginWindow CREATED ===");

            // Subscribe to events
            _viewModel.LoginSuccessful += OnLoginSuccessful;
            _viewModel.ShowError += OnShowError;

            // Focus username field
            Loaded += (s, e) =>
            {
                TxtUsername.Focus();
                _logger.LogInformation("   LoginWindow loaded, username field focused");
                Debug.WriteLine("   LoginWindow loaded, username field focused");
            };
        }

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = TxtPassword.Password;
            }
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("=== Login Button CLICKED ===");
            Debug.WriteLine("=== Login Button CLICKED ===");

            try
            {
                var success = await _viewModel.LoginWithPasswordAsync();

                if (!success)
                {
                    _logger.LogWarning("   Login attempt failed");
                    Debug.WriteLine("   Login attempt failed");
                }
                // If successful, OnLoginSuccessful will be called
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in LoginButton_Click");
                Debug.WriteLine($"❌ Exception in LoginButton_Click: {ex.Message}");
                MessageBox.Show($"Login error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BiometricLoginButton_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("=== Biometric Login Button CLICKED ===");
            Debug.WriteLine("=== Biometric Login Button CLICKED ===");

            try
            {
                var success = await _viewModel.LoginWithBiometricAsync();

                if (!success)
                {
                    _logger.LogWarning("   Biometric login attempt failed");
                    Debug.WriteLine("   Biometric login attempt failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in BiometricLoginButton_Click");
                Debug.WriteLine($"❌ Exception in BiometricLoginButton_Click: {ex.Message}");
                MessageBox.Show($"Biometric login error: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnLoginSuccessful(string authToken)
        {
            _logger.LogInformation("=== LOGIN SUCCESSFUL ===");
            _logger.LogInformation("   Auth Token Length: {Length}", authToken?.Length ?? 0);
            Debug.WriteLine("=== LOGIN SUCCESSFUL ===");
            Debug.WriteLine($"   Auth Token Length: {authToken?.Length ?? 0}");

            AuthToken = authToken;
            LoginSuccess = true;

            _logger.LogInformation("   Closing login window, DialogResult = true");
            Debug.WriteLine("   Closing login window, DialogResult = true");

            DialogResult = true;
            Close();
        }

        private void OnShowError(string title, string message)
        {
            _logger.LogWarning("=== Showing Error Dialog ===");
            _logger.LogWarning("   Title: {Title}", title);
            _logger.LogWarning("   Message: {Message}", message);
            Debug.WriteLine($"❌ Error: {title} - {message}");

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        protected override void OnClosed(EventArgs e)
        {
            _logger.LogInformation("=== LoginWindow CLOSED ===");
            _logger.LogInformation("   LoginSuccess: {Success}", LoginSuccess);
            _logger.LogInformation("   AuthToken: {HasToken}", !string.IsNullOrEmpty(AuthToken));
            Debug.WriteLine($"=== LoginWindow CLOSED ===");
            Debug.WriteLine($"   LoginSuccess: {LoginSuccess}");
            Debug.WriteLine($"   AuthToken: {!string.IsNullOrEmpty(AuthToken)}");

            // Unsubscribe from events
            _viewModel.LoginSuccessful -= OnLoginSuccessful;
            _viewModel.ShowError -= OnShowError;

            base.OnClosed(e);
        }
    }
}
