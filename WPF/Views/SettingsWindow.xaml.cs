using Microsoft.Extensions.Logging;
using System.Windows;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;
        private readonly ILogger<SettingsViewModel> _logger;

        public SettingsWindow(SettingsViewModel viewModel, ILogger<SettingsViewModel> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel = viewModel;
            _logger = logger;

            logger.LogInformation("Settings window opened");
            Closing += (s, e) => _logger.LogInformation("Settings window closing");
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate URL format
                if (!Uri.TryCreate(_viewModel.ApiBaseUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("Please enter a valid API URL.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save settings
                _viewModel.SaveSettings();

                // Test connection after saving (optional)
                if (_viewModel.TestConnectionAfterSave)
                {
                    await _viewModel.TestConnectionAfterSaveAsync();
                }

                // Close window with success
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel.CancelSettings();
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling settings");
                MessageBox.Show($"Error canceling settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            // Simple UI call - all logic is in ViewModel
            await _viewModel.TestConnectionAsync();
        }

        private void ApiBaseUrl_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // ViewModel handles this automatically via property setter
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up
            _viewModel.Cleanup();
            base.OnClosed(e);
        }
    }
}