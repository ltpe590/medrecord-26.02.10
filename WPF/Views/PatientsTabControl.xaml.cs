using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class PatientsTabControl : UserControl
    {
        // ── Event raised when user wants to start a visit ──────────────────────
        // MainWindow handles this: switches tab, calls SelectPatientAsync.
        public event EventHandler<PatientViewModel>? StartVisitRequested;

        private MainWindowViewModel? VM => DataContext as MainWindowViewModel;

        public PatientsTabControl()
        {
            InitializeComponent();
        }

        // ── List selection ──────────────────────────────────────────────────────

        private async void PatientListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VM == null) return;
            if (PatientListBox.SelectedItem is PatientViewModel patient)
                await VM.SelectPatientAsync(patient, forceNewVisit: false);
            else
                await VM.SelectPatientAsync(null);
        }

        private void PatientListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VM?.SelectedPatient == null) return;
            StartVisitRequested?.Invoke(this, VM.SelectedPatient);
        }

        // ── Context menu ────────────────────────────────────────────────────────

        private PatientViewModel? GetContextMenuPatient(object sender)
        {
            if (sender is MenuItem mi &&
                mi.Parent is ContextMenu cm &&
                cm.PlacementTarget is FrameworkElement fe &&
                fe.DataContext is PatientViewModel patient)
                return patient;
            return null;
        }

        private void ContextMenu_StartVisit_Click(object sender, RoutedEventArgs e)
        {
            var patient = GetContextMenuPatient(sender);
            if (patient != null)
                StartVisitRequested?.Invoke(this, patient);
        }

        private async void ContextMenu_EditPatient_Click(object sender, RoutedEventArgs e)
        {
            var patient = GetContextMenuPatient(sender);
            if (patient != null)
                await EditPatientAsync(patient);
        }

        private async void ContextMenu_DeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) return;
            var patient = GetContextMenuPatient(sender);
            if (patient == null) return;

            var result = MessageBox.Show(
                $"Delete patient \"{patient.Name}\"?\n\nThis cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
            await VM.DeletePatientAsync(patient);
        }

        // ── Edit dialog ─────────────────────────────────────────────────────────

        private async Task EditPatientAsync(PatientViewModel patient)
        {
            if (VM == null) return;
            try
            {
                var vm = VM.CreateRegisterPatientViewModel();
                vm.Name        = patient.Name;
                vm.DateOfBirth = patient.DateOfBirth;
                vm.Sex         = patient.SexDisplay;
                vm.PhoneNumber = patient.PhoneNumber;
                vm.Address     = patient.Address;
                vm.BloodGroup  = patient.BloodGroup;
                vm.Allergies   = patient.Allergies;

                var dialog = new RegisterPatientWindow(vm) { Owner = Window.GetWindow(this) };
                if (dialog.ShowDialog() != true || vm.CreatedPatient == null) return;

                await VM.UpdatePatientAsync(patient.PatientId, vm.CreatedPatient);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error Editing Patient",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}