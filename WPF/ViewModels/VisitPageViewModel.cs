using Core.DTOs;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WPF.ViewModels
{
    public class VisitPageViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private readonly int _patientId;
        private readonly string _patientName;
        private readonly IAppSettingsService _settings;
        private readonly IUserService _userService;
        private readonly IVisitService _visitService;
        private readonly ILogger<VisitPageViewModel> _logger;

        // Visit data
        private string _diagnosis = string.Empty;

        private string _notes = string.Empty;
        private decimal _temperature;
        private int _bpSystolic;
        private int _bpDiastolic;
        private int _gravida;
        private int _para;
        private int _abortion;
        private DateTime? _lmpDate;

        // Status tracking
        private VisitStatus _visitStatus = VisitStatus.InProgress;

        private DateTime _visitStartTime;
        private DateTime? _lastSavedTime;
        private string _statusMessage = string.Empty;

        // Collections
        private List<TestCatalogDto> _availableTests = new();

        private List<DrugCatalogDto> _availableDrugs = new();
        private List<LabResultEntry> _labResults = new();
        private List<PrescriptionEntry> _prescriptions = new();

        // Selected items
        private TestCatalogDto? _selectedLabTest;

        private DrugCatalogDto? _selectedDrug;
        private string _labResultValue = string.Empty;
        private string _dosage = string.Empty;

        #endregion Private Fields

        public VisitPageViewModel(
            int patientId,
            string patientName,
            IAppSettingsService settings,
            IUserService userService,
            IVisitService visitService,
            ILogger<VisitPageViewModel> logger)
        {
            _patientId = patientId;
            _patientName = patientName;
            _settings = settings;
            _userService = userService;
            _visitService = visitService;
            _logger = logger;
            _visitStartTime = DateTime.Now;

            StatusMessage = "Visit started";
        }

        #region Properties

        public string Diagnosis
        {
            get => _diagnosis;
            set { _diagnosis = value; OnPropertyChanged(); }
        }

        public string Notes
        {
            get => _notes;
            set { _notes = value; OnPropertyChanged(); }
        }

        public decimal Temperature
        {
            get => _temperature;
            set { _temperature = value; OnPropertyChanged(); }
        }

        public int BPSystolic
        {
            get => _bpSystolic;
            set { _bpSystolic = value; OnPropertyChanged(); }
        }

        public int BPDiastolic
        {
            get => _bpDiastolic;
            set { _bpDiastolic = value; OnPropertyChanged(); }
        }

        public int Gravida
        {
            get => _gravida;
            set { _gravida = value; OnPropertyChanged(); }
        }

        public int Para
        {
            get => _para;
            set { _para = value; OnPropertyChanged(); }
        }

        public int Abortion
        {
            get => _abortion;
            set { _abortion = value; OnPropertyChanged(); }
        }

        public DateTime? LMPDate
        {
            get => _lmpDate;
            set
            {
                _lmpDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(EDDText));
            }
        }

        public string EDDText
        {
            get
            {
                if (LMPDate.HasValue)
                {
                    var edd = LMPDate.Value.AddDays(280);
                    return $"EDD: {edd:dd MMM yyyy}";
                }
                return string.Empty;
            }
        }

        public VisitStatus VisitStatus
        {
            get => _visitStatus;
            set { _visitStatus = value; OnPropertyChanged(); }
        }

        public string LastSavedTime
        {
            get => _lastSavedTime?.ToString("HH:mm:ss") ?? "Not saved";
        }

        public string VisitDuration
        {
            get
            {
                var duration = DateTime.Now - _visitStartTime;
                return $"{duration.Hours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public List<TestCatalogDto> AvailableTests
        {
            get => _availableTests;
            set { _availableTests = value; OnPropertyChanged(); }
        }

        public List<DrugCatalogDto> AvailableDrugs
        {
            get => _availableDrugs;
            set { _availableDrugs = value; OnPropertyChanged(); }
        }

        public List<LabResultEntry> LabResults
        {
            get => _labResults;
            set { _labResults = value; OnPropertyChanged(); }
        }

        public List<PrescriptionEntry> Prescriptions
        {
            get => _prescriptions;
            set { _prescriptions = value; OnPropertyChanged(); }
        }

        public TestCatalogDto? SelectedLabTest
        {
            get => _selectedLabTest;
            set { _selectedLabTest = value; OnPropertyChanged(); }
        }

        public DrugCatalogDto? SelectedDrug
        {
            get => _selectedDrug;
            set { _selectedDrug = value; OnPropertyChanged(); }
        }

        public string LabResultValue
        {
            get => _labResultValue;
            set { _labResultValue = value; OnPropertyChanged(); }
        }

        public string Dosage
        {
            get => _dosage;
            set { _dosage = value; OnPropertyChanged(); }
        }

        #endregion Properties

        #region Public Methods

        public async Task InitializeAsync(string authToken) // Add authToken parameter
        {
            try
            {
                StatusMessage = "Loading data...";

                if (string.IsNullOrEmpty(authToken))
                {
                    StatusMessage = "Authentication required";
                    return;
                }

                // Load available tests and drugs using the actual services
                var tasks = new List<Task>
        {
            LoadTestCatalogAsync(authToken, _logger),
            LoadDrugCatalogAsync(authToken, _logger)
        };

                await Task.WhenAll(tasks);

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
                MessageBox.Show($"Error loading visit data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void AddLabResult()
        {
            if (SelectedLabTest != null && !string.IsNullOrWhiteSpace(LabResultValue))
            {
                var newResult = new LabResultEntry
                {
                    TestId = SelectedLabTest.TestId,
                    TestName = SelectedLabTest.TestName,
                    ResultValue = LabResultValue,
                    TestUnit = SelectedLabTest.TestUnit,
                    NormalRange = SelectedLabTest.NormalRange
                };

                LabResults.Add(newResult);
                LabResults = new List<LabResultEntry>(LabResults);
                LabResultValue = string.Empty;
                SelectedLabTest = null;
                StatusMessage = "Lab result added";
            }
        }

        public void RemoveLabResult(object item)
        {
            if (item is LabResultEntry result)
            {
                LabResults.Remove(result);
                LabResults = new List<LabResultEntry>(LabResults);
                StatusMessage = "Lab result removed";
            }
        }

        public void AddPrescription()
        {
            if (SelectedDrug != null && !string.IsNullOrWhiteSpace(Dosage))
            {
                var newPrescription = new PrescriptionEntry
                {
                    DrugId = SelectedDrug.DrugId,
                    DrugName = SelectedDrug.BrandName,
                    Dosage = Dosage,
                    Form = SelectedDrug.Form
                };

                Prescriptions.Add(newPrescription);
                Prescriptions = new List<PrescriptionEntry>(Prescriptions);
                Dosage = string.Empty;
                SelectedDrug = null;
                StatusMessage = "Prescription added";
            }
        }

        public void RemovePrescription(object item)
        {
            if (item is PrescriptionEntry prescription)
            {
                Prescriptions.Remove(prescription);
                Prescriptions = new List<PrescriptionEntry>(Prescriptions);
                StatusMessage = "Prescription removed";
            }
        }

        public bool HasUnsavedChanges()
        {
            return !string.IsNullOrWhiteSpace(Diagnosis) ||
                   !string.IsNullOrWhiteSpace(Notes) ||
                   LabResults.Any() ||
                   Prescriptions.Any() ||
                   Temperature > 0 ||
                   BPSystolic > 0 ||
                   BPDiastolic > 0;
        }

        public async Task SaveVisitAsync(string authToken)
        {
            if (string.IsNullOrEmpty(authToken))
            {
                StatusMessage = "Authentication required";
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            try
            {
                StatusMessage = "Saving visit...";
                VisitStatus = VisitStatus.Saving;

                // Create VisitSaveRequest (used by IVisitService)
                var request = new VisitSaveRequest
                {
                    PatientId = _patientId,
                    PatientName = _patientName,
                    AuthToken = authToken,
                    Diagnosis = Diagnosis,
                    Notes = Notes,
                    Temperature = Temperature,
                    BloodPressureSystolic = BPSystolic,
                    BloodPressureDiastolic = BPDiastolic,
                    Gravida = Gravida,
                    Para = Para,
                    Abortion = Abortion,
                    LMPDate = LMPDate,
                    SaveType = VisitSaveType.New
                };

                // Use VisitService which handles everything
                var result = await _visitService.SaveVisitAsync(request);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Message);
                }

                VisitStatus = VisitStatus.Completed;
                _lastSavedTime = DateTime.Now;
                OnPropertyChanged(nameof(LastSavedTime));

                StatusMessage = "Visit saved successfully";
            }
            catch (Exception ex)
            {
                VisitStatus = VisitStatus.Error;
                StatusMessage = $"Error saving visit: {ex.Message}";
                throw;
            }
        }

        public void CancelVisit()
        {
            try
            {
                // Clear all data
                Diagnosis = string.Empty;
                Notes = string.Empty;
                Temperature = 0;
                BPSystolic = 0;
                BPDiastolic = 0;
                Gravida = 0;
                Para = 0;
                Abortion = 0;
                LMPDate = null;
                LabResults.Clear();
                Prescriptions.Clear();

                // In real app: Remove any saved drafts
                VisitStatus = VisitStatus.Cancelled;
                StatusMessage = "Visit cancelled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error canceling visit: {ex.Message}";
                throw;
            }
        }

        #endregion Public Methods

        #region Helper Methods

        public sealed class LabResultEntry
        {
            public int TestId { get; set; }
            public string TestName { get; set; } = string.Empty;
            public string ResultValue { get; set; } = string.Empty;
            public string? TestUnit { get; set; }
            public string? NormalRange { get; set; }
        }

        public sealed class PrescriptionEntry
        {
            public int DrugId { get; set; }
            public string DrugName { get; set; } = string.Empty;
            public string Dosage { get; set; } = string.Empty;
            public string? Form { get; set; }
        }

        private async Task LoadTestCatalogAsync(string authToken, ILogger<VisitPageViewModel> logger)
        {
            try
            {
                AvailableTests = await _userService.GetTestCatalogAsync(_settings.ApiBaseUrl, authToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load Testcatalog; falling back to mock data");
                LoadMockData();
                StatusMessage = "Using offline test data";
            }
        }

        private async Task LoadDrugCatalogAsync(string authToken, ILogger<VisitPageViewModel> logger)
        {
            try
            {
                AvailableDrugs = await _userService.GetDrugCatalogAsync(_settings.ApiBaseUrl, authToken);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load Drugcatalog; falling back to mock data");
                LoadMockData();
                StatusMessage = $"Using offline drug data";
            }
        }

        private async Task SaveLabResultsAsync(string authToken)
        {
            foreach (var labResult in LabResults)
            {
                var labDto = new LabResultCreateDto
                {
                    TestId = labResult.TestId,
                    VisitId = GetCurrentVisitId(), // You'll need to track visit ID
                    ResultValue = labResult.ResultValue
                };
                await _userService.SaveLabResultAsync(labDto, _settings.ApiBaseUrl, authToken);
            }
        }

        private async Task SavePrescriptionsAsync(string authToken)
        {
            foreach (var prescription in Prescriptions)
            {
                var prescriptionDto = new PrescriptionCreateDto
                {
                    VisitId = GetCurrentVisitId(), // You'll need to track visit ID
                    DrugId = prescription.DrugId,
                    Dosage = prescription.Dosage,
                    DurationDays = "7 days" // Default value
                };
                await _userService.SavePrescriptionAsync(prescriptionDto, _settings.ApiBaseUrl, authToken);
            }
        }

        private int GetCurrentVisitId()
        {
            // You need to track the current visit ID
            // For now, return 0 or implement proper visit ID tracking
            return 0; // Placeholder
        }

        #endregion Helper Methods

        #region Private Methods

        private void LoadMockData()
        {
            // Mock lab tests
            AvailableTests = new List<TestCatalogDto>
            {
                new TestCatalogDto { TestId = 1, TestName = "Complete Blood Count", TestUnit = "cells/µL", NormalRange = "4.5-11.0" },
                new TestCatalogDto { TestId = 2, TestName = "Glucose Fasting", TestUnit = "mg/dL", NormalRange = "70-100" },
                new TestCatalogDto { TestId = 3, TestName = "Cholesterol Total", TestUnit = "mg/dL", NormalRange = "<200" }
            };

            // Mock drugs
            AvailableDrugs = new List<DrugCatalogDto>
            {
                new DrugCatalogDto { DrugId = 1, BrandName = "Paracetamol", Form = "Tablet", DosageStrength = "500mg" },
                new DrugCatalogDto { DrugId = 2, BrandName = "Amoxicillin", Form = "Capsule", DosageStrength = "250mg" },
                new DrugCatalogDto { DrugId = 3, BrandName = "Metformin", Form = "Tablet", DosageStrength = "500mg" }
            };
        }

        #endregion Private Methods

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }

    #region Helper Classes

    public enum VisitStatus
    {
        InProgress,
        Paused,
        Saving,
        Completed,
        Cancelled,
        Error
    }

    #endregion Helper Classes
}