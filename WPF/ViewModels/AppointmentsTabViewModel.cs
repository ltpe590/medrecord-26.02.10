using Core.DTOs;
using WPF.Mappers;
using Core.Entities;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace WPF.ViewModels
{
    /// <summary>
    /// Drives the Appointments tab.
    /// LoadDayAsync is called once when the tab is first shown, then on every date change.
    /// </summary>
    public sealed class AppointmentsTabViewModel : BaseViewModel
    {
        private readonly IAppointmentService               _svc;
        private readonly IPatientService                   _patientSvc;
        private readonly ILogger<AppointmentsTabViewModel> _logger;

        // ── State ─────────────────────────────────────────────────────────────
        private DateTime          _selectedDate      = DateTime.Today;
        private bool              _isLoading;
        private string            _statusMessage     = string.Empty;

        // ── New appointment form ──────────────────────────────────────────────
        private PatientViewModel? _newApptPatient;
        private DateTime          _newApptDate       = DateTime.Today;
        private string            _newApptTimeText   = "09:00";
        private string            _newApptReason     = string.Empty;
        private string            _patientSearchText = string.Empty;

        public AppointmentsTabViewModel(
            IAppointmentService               svc,
            IPatientService                   patientSvc,
            ILogger<AppointmentsTabViewModel> logger)
        {
            _svc        = svc;
            _patientSvc = patientSvc;
            _logger     = logger;
        }

        // ── Observable collections ────────────────────────────────────────────
        public ObservableCollection<AppointmentDto>  DayAppointments    { get; } = new();
        public ObservableCollection<PatientViewModel> PatientSuggestions { get; } = new();

        // ── Properties ────────────────────────────────────────────────────────
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set { if (SetProperty(ref _selectedDate, value)) _ = LoadDayAsync(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public PatientViewModel? NewApptPatient
        {
            get => _newApptPatient;
            set { SetProperty(ref _newApptPatient, value); OnPropertyChanged(nameof(CanAddAppointment)); }
        }

        public DateTime NewApptDate
        {
            get => _newApptDate;
            set { SetProperty(ref _newApptDate, value); OnPropertyChanged(nameof(CanAddAppointment)); }
        }

        /// Free-text time entry, e.g. "09:30". Validated on submit.
        public string NewApptTimeText
        {
            get => _newApptTimeText;
            set => SetProperty(ref _newApptTimeText, value);
        }

        public string NewApptReason
        {
            get => _newApptReason;
            set => SetProperty(ref _newApptReason, value);
        }

        public string PatientSearchText
        {
            get => _patientSearchText;
            set { SetProperty(ref _patientSearchText, value); _ = SearchPatientsAsync(value); }
        }

        public bool CanAddAppointment =>
            NewApptPatient != null && NewApptDate.Date >= DateTime.Today;

        // ── Events (subscribed by code-behind) ────────────────────────────────
        public event Action<string, string>? OnShowError;
        public event Action<string>?         OnShowSuccess;

        // ── Public methods ────────────────────────────────────────────────────
        public Task InitAsync() => LoadDayAsync();

        public async Task LoadDayAsync()
        {
            IsLoading = true;
            try
            {
                var list = await _svc.GetForDayAsync(SelectedDate);
                DayAppointments.Clear();
                foreach (var dto in list) DayAppointments.Add(dto);
                StatusMessage = DayAppointments.Count == 0
                    ? "No appointments for this day."
                    : $"{DayAppointments.Count} appointment(s)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load appointments for {Date}", SelectedDate);
                StatusMessage = "Failed to load appointments.";
            }
            finally { IsLoading = false; }
        }

        public async Task AddAppointmentAsync()
        {
            if (!CanAddAppointment || NewApptPatient == null) return;

            if (!TimeSpan.TryParseExact(NewApptTimeText.Trim(), @"hh\:mm",
                    System.Globalization.CultureInfo.InvariantCulture, out var time))
            {
                OnShowError?.Invoke("Invalid Time", "Enter time as HH:mm, e.g. 09:30");
                return;
            }

            var scheduledAt = NewApptDate.Date.Add(time);

            try
            {
                await _svc.CreateAsync(new AppointmentCreateDto
                {
                    PatientId   = NewApptPatient.PatientId,
                    ScheduledAt = scheduledAt,
                    Reason      = string.IsNullOrWhiteSpace(NewApptReason) ? null : NewApptReason.Trim()
                });

                OnShowSuccess?.Invoke($"Appointment booked for {NewApptPatient.Name} at {scheduledAt:HH:mm}");
                ClearForm();

                // Refresh if the booked day is currently displayed
                if (NewApptDate.Date == SelectedDate.Date || scheduledAt.Date == SelectedDate.Date)
                    await LoadDayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create appointment");
                OnShowError?.Invoke("Booking Error", ex.Message);
            }
        }

        public async Task CancelAppointmentAsync(AppointmentDto dto)
        {
            try
            {
                await _svc.CancelAsync(dto.AppointmentId);
                OnShowSuccess?.Invoke("Appointment cancelled.");
                await LoadDayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel appointment {Id}", dto.AppointmentId);
                OnShowError?.Invoke("Cancel Error", ex.Message);
            }
        }

        public async Task MarkArrivedAsync(AppointmentDto dto)
        {
            try
            {
                await _svc.UpdateStatusAsync(dto.AppointmentId, AppointmentStatus.Arrived);
                await LoadDayAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark arrived {Id}", dto.AppointmentId);
                OnShowError?.Invoke("Error", ex.Message);
            }
        }

        public void SelectPatientSuggestion(PatientViewModel patient)
        {
            NewApptPatient    = patient;
            PatientSearchText = patient.Name;
            PatientSuggestions.Clear();
        }

        // ── Private helpers ───────────────────────────────────────────────────
        private async Task SearchPatientsAsync(string text)
        {
            PatientSuggestions.Clear();
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return;

            try
            {
                var all = await _patientSvc.GetAllPatientsListAsync();
                var matches = all
                    .Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .Take(8)
                    .Select(p => PatientMapper.ToViewModel(p));

                foreach (var m in matches) PatientSuggestions.Add(m);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Patient search failed");
            }
        }

        private void ClearForm()
        {
            NewApptPatient    = null;
            PatientSearchText = string.Empty;
            NewApptDate       = DateTime.Today;
            NewApptTimeText   = "09:00";
            NewApptReason     = string.Empty;
            PatientSuggestions.Clear();
        }
    }
}
