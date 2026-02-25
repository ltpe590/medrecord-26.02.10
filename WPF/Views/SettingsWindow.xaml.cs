using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _viewModel;
        private readonly ILogger<SettingsWindow> _logger;

        // Track which panel is currently visible
        private UIElement? _activePanel;

        public SettingsWindow(SettingsViewModel viewModel, ILogger<SettingsWindow> logger)
        {
            InitializeComponent();
            DataContext = viewModel;
            _viewModel  = viewModel;
            _logger     = logger;

            Loaded  += OnWindowLoaded;
            Closing += (s, e) => _logger.LogInformation("Settings window closing");
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("Settings window opened");

            // Subscribe to preview accent changes
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // Show the connection tab by default
            ShowPanel(PanelConn);

            // Load catalogs if we have a token (set before ShowDialog)
            if (string.IsNullOrEmpty(_viewModel.AuthToken))
            {
                _logger.LogWarning("SettingsWindow opened without auth token — Lab and Pharmacy catalogs will not load");
            }
            else
            {
                await _viewModel.LoadTestsAsync();
                await _viewModel.LoadDrugsAsync();
            }
        }

        // ── Token injection (called by MainWindowViewModel before ShowDialog) ──
        public void SetAuthToken(string token) => _viewModel.SetAuthToken(token);

        // ════════════════════════════════════════════════════════════════════════
        // TAB SWITCHING
        // ════════════════════════════════════════════════════════════════════════

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            // Guard: XAML named elements are null during InitializeComponent — skip early events
            if (!IsInitialized) return;
            if (sender is not RadioButton rb) return;

            UIElement? target = rb.Name switch
            {
                "TabConn"     => PanelConn,
                "TabLab"      => PanelLab,
                "TabPharmacy" => PanelPharmacy,
                "TabDoctor"   => PanelDoctor,
                "TabAppear"   => PanelAppear,
                "TabAi"       => PanelAi,
                _             => PanelConn
            };

            if (target is not null)
                ShowPanel(target);
        }

        private void ShowPanel(UIElement panel)
        {
            // Guard: panels may be null before InitializeComponent completes
            if (PanelConn is null) return;

            PanelConn.Visibility     = Visibility.Collapsed;
            PanelLab.Visibility      = Visibility.Collapsed;
            PanelPharmacy.Visibility = Visibility.Collapsed;
            PanelDoctor.Visibility   = Visibility.Collapsed;
            PanelAppear.Visibility   = Visibility.Collapsed;
            PanelAi.Visibility       = Visibility.Collapsed;

            panel.Visibility = Visibility.Visible;
            _activePanel = panel;
        }

        // ════════════════════════════════════════════════════════════════════════
        // SAVE / CANCEL
        // ════════════════════════════════════════════════════════════════════════

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
                _logger.LogError(ex, "Error canceling settings");
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // CONNECTION TAB
        // ════════════════════════════════════════════════════════════════════════

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.TestConnectionAsync();

        // ════════════════════════════════════════════════════════════════════════
        // LAB TAB
        // ════════════════════════════════════════════════════════════════════════

        private async void RefreshTestsButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.LoadTestsAsync();

        private async void AddTestButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.AddTestAsync();

        private async void DeleteTestButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.DeleteTestAsync();

        // ════════════════════════════════════════════════════════════════════════
        // PHARMACY TAB
        // ════════════════════════════════════════════════════════════════════════

        private async void RefreshDrugsButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.LoadDrugsAsync();

        private async void AddDrugButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.AddDrugAsync();

        private async void DeleteDrugButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.DeleteDrugAsync();

        // ════════════════════════════════════════════════════════════════════════
        // APPEARANCE TAB
        // ════════════════════════════════════════════════════════════════════════

        private void PreviewThemeButton_Click(object sender, RoutedEventArgs e)
            => _viewModel.PreviewTheme();

        // ════════════════════════════════════════════════════════════════════════
        // AI TAB
        // ════════════════════════════════════════════════════════════════════════

        private async void TestAiButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.TestAiProviderAsync();

        private async void DetectOllamaButton_Click(object sender, RoutedEventArgs e)
            => await _viewModel.DetectOllamaAsync();

        // ════════════════════════════════════════════════════════════════════════
        // CLEANUP
        // ════════════════════════════════════════════════════════════════════════

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Cleanup();
            base.OnClosed(e);
        }

        // Updates the accent preview swatch without needing a custom converter
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SettingsViewModel.PreviewAccentHex)) return;
            try
            {
                var brush = (SolidColorBrush)new BrushConverter()
                    .ConvertFromString(_viewModel.PreviewAccentHex)!;
                AccentPreviewBorder.Background = brush;
                AccentPreviewText.Text = _viewModel.PreviewAccentHex;
            }
            catch { /* ignore bad hex */ }
        }
    }
}
