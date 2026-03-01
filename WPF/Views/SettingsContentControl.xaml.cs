using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF.ViewModels;

namespace WPF.Views
{
    /// <summary>
    /// Shared settings content panel — hosted by both SettingsWindow (popup) and
    /// the Settings tab inside MainWindow. Contains the tab bar + all setting panels.
    /// Save / Cancel actions are handled by each host independently.
    /// </summary>
    public partial class SettingsContentControl : UserControl
    {
        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        public SettingsContentControl()
        {
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;

            VM.PropertyChanged += OnViewModelPropertyChanged;
            ShowPanel(PanelConn);

            if (!string.IsNullOrEmpty(VM.AuthToken))
            {
                await VM.LoadTestsAsync();
                await VM.LoadDrugsAsync();
            }
        }

        // Called by host after injecting auth token
        public void NotifyTokenReady() => _ = InitCatalogsAsync();

        private async Task InitCatalogsAsync()
        {
            if (VM == null || string.IsNullOrEmpty(VM.AuthToken)) return;
            await VM.LoadTestsAsync();
            await VM.LoadDrugsAsync();
        }

        // ── Tab switching ─────────────────────────────────────────────────────
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized || sender is not RadioButton rb) return;
            UIElement? target = rb.Name switch
            {
                "TabConn"     => PanelConn,
                "TabLab"      => (UIElement?)PanelLab,
                "TabPharmacy" => PanelPharmacy,
                "TabDoctor"   => PanelDoctor,
                "TabAppear"   => PanelAppear,
                "TabAi"       => PanelAi,
                _             => PanelConn
            };
            if (target != null) ShowPanel(target);
        }

        internal void ShowPanel(UIElement panel)
        {
            if (PanelConn is null) return;
            PanelConn.Visibility     = Visibility.Collapsed;
            PanelLab.Visibility      = Visibility.Collapsed;
            PanelPharmacy.Visibility = Visibility.Collapsed;
            PanelDoctor.Visibility   = Visibility.Collapsed;
            PanelAppear.Visibility   = Visibility.Collapsed;
            PanelAi.Visibility       = Visibility.Collapsed;
            panel.Visibility         = Visibility.Visible;
        }

        // ── Connection tab ────────────────────────────────────────────────────
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.TestConnectionAsync();
        }

        // ── Lab tab ───────────────────────────────────────────────────────────
        private async void RefreshTestsButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.LoadTestsAsync();
        }
        private async void AddTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.AddTestAsync();
        }
        private async void DeleteTestButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.DeleteTestAsync();
        }

        // ── Pharmacy tab ──────────────────────────────────────────────────────
        private async void RefreshDrugsButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.LoadDrugsAsync();
        }
        private async void AddDrugButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.AddDrugAsync();
        }
        private async void DeleteDrugButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.DeleteDrugAsync();
        }

        // ── Appearance tab ────────────────────────────────────────────────────
        private void PreviewThemeButton_Click(object sender, RoutedEventArgs e) =>
            VM?.PreviewTheme();

        // ── AI tab ────────────────────────────────────────────────────────────
        private async void TestAiButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.TestAiProviderAsync();
        }
        private async void DetectOllamaButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.DetectOllamaAsync();
        }

        // ── Cleanup ───────────────────────────────────────────────────────────
        public void Detach()
        {
            if (VM != null) VM.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SettingsViewModel.PreviewAccentHex)) return;
            try
            {
                var brush = (SolidColorBrush)new BrushConverter()
                    .ConvertFromString(VM!.PreviewAccentHex)!;
                AccentPreviewBorder.Background = brush;
                AccentPreviewText.Text         = VM.PreviewAccentHex;
            }
            catch { /* ignore invalid hex */ }
        }
    }
}

