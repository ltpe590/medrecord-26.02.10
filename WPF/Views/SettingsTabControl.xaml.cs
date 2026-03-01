using System;
using System.Windows;
using System.Windows.Controls;
using WPF.ViewModels;

namespace WPF.Views
{
    /// <summary>
    /// Thin host for SettingsContentControl embedded as the fourth main tab.
    /// Owns Save / Discard for the inline (non-dialog) context.
    /// DataContext must be set to <see cref="SettingsViewModel"/> by the parent before load.
    /// </summary>
    public partial class SettingsTabControl : UserControl
    {
        private SettingsViewModel? VM => DataContext as SettingsViewModel;

        public SettingsTabControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded           += OnUnloaded;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Unsubscribe from old VM
            if (e.OldValue is SettingsViewModel old)
            {
                old.OnShowInfo      -= ShowInfo;
                old.OnShowError     -= ShowError;
                old.OnShowWarning   -= ShowWarning;
                old.OnConfirmDialog -= Confirm;
            }

            // Forward the same VM to the shared content control
            SettingsContent.DataContext = e.NewValue;

            // Subscribe to new VM so MessageBox dialogs work in tab context
            if (e.NewValue is SettingsViewModel vm)
            {
                vm.OnShowInfo      += ShowInfo;
                vm.OnShowError     += ShowError;
                vm.OnShowWarning   += ShowWarning;
                vm.OnConfirmDialog += Confirm;
            }
        }

        // ── VM dialog callbacks ───────────────────────────────────────────────
        private void ShowInfo(string title, string msg) =>
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Information);

        private void ShowError(string title, string msg) =>
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Error);

        private void ShowWarning(string title, string msg) =>
            MessageBox.Show(msg, title, MessageBoxButton.OK, MessageBoxImage.Warning);

        private bool Confirm(string title, string msg) =>
            MessageBox.Show(msg, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

        // Called by MainWindow after auth token is available
        public void SetAuthToken(string token)
        {
            VM?.SetAuthToken(token);
            SettingsContent.NotifyTokenReady();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            try
            {
                if (!Uri.TryCreate(VM.ApiBaseUrl, UriKind.Absolute, out _))
                {
                    ShowStatus("⚠ Invalid API URL — not saved.", "#F44336");
                    return;
                }

                VM.SaveSettings();

                if (VM.TestConnectionAfterSave)
                    await VM.TestConnectionAfterSaveAsync();

                ShowStatus("✓ Settings saved.", "#4CAF50");
            }
            catch (Exception ex)
            {
                ShowStatus($"✕ Error: {ex.Message}", "#F44336");
            }
        }

        private void DiscardButton_Click(object sender, RoutedEventArgs e)
        {
            VM?.CancelSettings();
            ShowStatus("Changes discarded.", "#9E9E9E");
        }

        private void ShowStatus(string message, string hexColour)
        {
            TxtSaveStatus.Text       = message;
            TxtSaveStatus.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hexColour));
        }

        // Forward Detach to the shared control on unload so event subscriptions are cleaned up
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (VM != null)
            {
                VM.OnShowInfo      -= ShowInfo;
                VM.OnShowError     -= ShowError;
                VM.OnShowWarning   -= ShowWarning;
                VM.OnConfirmDialog -= Confirm;
            }
            SettingsContent.Detach();
        }
    }
}
