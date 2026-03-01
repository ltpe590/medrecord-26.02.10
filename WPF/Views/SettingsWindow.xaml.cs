using Microsoft.Extensions.Logging;
using System.Windows;
using WPF.ViewModels;

namespace WPF.Views
{
    /// <summary>
    /// Thin host for SettingsContentControl inside a dialog Window.
    /// All settings panel handlers live in SettingsContentControl.xaml.cs.
    /// This class only owns Save / Cancel (dialog-specific).
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel       _viewModel;
        private readonly ILogger<SettingsWindow> _logger;

        public SettingsWindow(SettingsViewModel viewModel, ILogger<SettingsWindow> logger)
        {
            InitializeComponent();

            _viewModel  = viewModel;
            _logger     = logger;

            DataContext                  = viewModel;
            SettingsContent.DataContext  = viewModel;

            _viewModel.OnShowInfo    += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Information);
            _viewModel.OnShowError   += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.OnShowWarning += (t, m) => MessageBox.Show(m, t, MessageBoxButton.OK, MessageBoxImage.Warning);
            _viewModel.OnConfirmDialog += (t, m) =>
                MessageBox.Show(m, t, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

            Closing += (s, e) =>
            {
                SettingsContent.Detach();
                _logger.LogInformation("Settings window closing");
            };
        }

        public void SetAuthToken(string token)
        {
            _viewModel.SetAuthToken(token);
            SettingsContent.NotifyTokenReady();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Uri.TryCreate(_viewModel.ApiBaseUrl, UriKind.Absolute, out _))
                {
                    MessageBox.Show("Please enter a valid API URL.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _viewModel.SaveSettings();

                if (_viewModel.TestConnectionAfterSave)
                    await _viewModel.TestConnectionAfterSaveAsync();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                MessageBox.Show($"Error saving settings:\n{ex.Message}", "Error",
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
                _logger.LogError(ex, "Error cancelling settings");
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
