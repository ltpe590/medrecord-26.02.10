using Core.DTOs;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using WPF.ViewModels;

namespace WPF.Views
{
    public partial class AppointmentsTabControl : UserControl
    {
        private AppointmentsTabViewModel? VM => DataContext as AppointmentsTabViewModel;

        public AppointmentsTabControl()
        {
            InitializeComponent();
        }

        // ── Date nav ─────────────────────────────────────────────────────────
        private void PrevDay_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.SelectedDate = VM.SelectedDate.AddDays(-1);
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.SelectedDate = VM.SelectedDate.AddDays(1);
        }

        private void Today_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) VM.SelectedDate = DateTime.Today;
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.LoadDayAsync();
        }

        // ── Patient autocomplete ──────────────────────────────────────────────
        private void TxtPatientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (VM == null) return;
            PatientSuggestPopup.IsOpen = VM.PatientSuggestions.Count > 0;
        }

        private void PatientSuggestList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientSuggestList.SelectedItem is PatientViewModel patient && VM != null)
            {
                VM.SelectPatientSuggestion(patient);
                PatientSuggestPopup.IsOpen = false;
            }
        }

        // ── Appointment actions ───────────────────────────────────────────────
        private async void BookAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null) await VM.AddAppointmentAsync();
        }

        private async void CancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null || ((FrameworkElement)sender).Tag is not AppointmentDto dto) return;

            var result = MessageBox.Show(
                $"Cancel appointment for {dto.PatientName} at {dto.ScheduledAt:HH:mm}?",
                "Confirm Cancellation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                await VM.CancelAppointmentAsync(dto);
        }

        private async void MarkArrived_Click(object sender, RoutedEventArgs e)
        {
            if (VM != null && ((FrameworkElement)sender).Tag is AppointmentDto dto)
                await VM.MarkArrivedAsync(dto);
        }
    }
}
