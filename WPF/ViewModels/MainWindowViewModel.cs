using Core.DTOs;
using Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using WPF.Mappers;
using WPF.Views;

namespace WPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        #region Services and Dependencies

        private readonly IUserService _userService;
        private readonly IPatientService _patientService;
        private readonly IVisitService _visitService;
        private readonly IConnectionService _connectionService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IAppSettingsService _settings;

        #endregion Services and Dependencies

        #region Private Fields

        private ObservableCollection<PatientViewModel> _patients = new();
        private PatientViewModel? _selectedPatient;
        private List<TestCatalogDto> _availableTests = new();
        private TestCatalogDto? _selectedTest;
        private string _authToken;
        private string? _username;
        private string? _password;
        private string _apiUrl = string.Empty;
        private string _statusMessage = "Ready";
        private string _visitHeaderText = "Visit";
        private string _patientSearchText = string.Empty;
        private string _patientHistory = string.Empty;
        private string _selectedPatientInfo = string.Empty;
        private string _selectedPatientDetails = string.Empty;
        private string _labResultValue = string.Empty;
        private string _diagnosis = string.Empty;
        private string _notes = string.Empty;
        private decimal _temperature;
        private int _currentVisitId;
        private int _bpSystolic;
        private int _bpDiastolic;
        private int _gravida;
        private int _para;
        private int _abortion;
        private bool _saveButtonEnabled;
        private bool _visitStarting;
        private bool _isPatientExpanderOpen = true;
        private bool _isVisitExpanderOpen;

        #endregion Private Fields

        #region Events

        public event Action<string, string>? OnShowErrorMessage;

        public event Action<string>? OnShowSuccessMessage;

        public event Func<Task>? SaveVisitRequested;

        public event Func<Task>? LoginCompleted;

        public event Func<Task>? PatientsLoaded;

        public event Func<Task>? PatientSelected;

        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion Events

        #region Boolean Properties

        public bool IsPatientExpanderOpen
        { get => _isPatientExpanderOpen; set { _isPatientExpanderOpen = value; OnPropertyChanged(); } }
        public bool IsVisitExpanderOpen
        { get => _isVisitExpanderOpen; set { _isVisitExpanderOpen = value; OnPropertyChanged(); } }
        public bool CanAddLabResult => _currentVisitId > 0 && SelectedPatient != null;
        public bool CanSaveVisit => SelectedPatient != null && !string.IsNullOrWhiteSpace(Diagnosis);

        #endregion Boolean Properties

        #region Public Properties

        #region Login Properties

        public string? Username
        { get => _username; set { if (_username != value) { _username = value; OnPropertyChanged(); } } }
        public string? Password
        { get => _password; set { if (_password != value) { _password = value; OnPropertyChanged(); } } }
        public string ApiUrl
        { get => _apiUrl; set { if (_apiUrl != value) { _apiUrl = value; OnPropertyChanged(); } } }

        #endregion Login Properties

        #region Status Properties

        public string StatusMessage
        { get => _statusMessage; private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(nameof(StatusMessage)); } } }
        public bool SaveButtonEnabled
        { get => _saveButtonEnabled; set { if (_saveButtonEnabled != value) { _saveButtonEnabled = value; OnPropertyChanged(nameof(SaveButtonEnabled)); } } }

        #endregion Status Properties

        #region Patient Properties

        public ObservableCollection<PatientViewModel> Patients { get; } = new();
        public PatientViewModel? SelectedPatient
        { get => _selectedPatient; set { if (_selectedPatient == value) return; _selectedPatient = value; OnPropertyChanged(); } }
        public string PatientSearchText
        { get => _patientSearchText; set { if (_patientSearchText != value) { _patientSearchText = value; OnPropertyChanged(nameof(PatientSearchText)); OnPropertyChanged(nameof(FilteredPatients)); } } }
        public string PatientHistory
        { get => _patientHistory; private set { if (_patientHistory != value) { _patientHistory = value; OnPropertyChanged(nameof(PatientHistory)); } } }
        public string SelectedPatientInfo
        { get => _selectedPatientInfo; private set { if (_selectedPatientInfo != value) { _selectedPatientInfo = value; OnPropertyChanged(nameof(SelectedPatientInfo)); } } }
        public string SelectedPatientDetails
        { get => _selectedPatientDetails; private set { if (_selectedPatientDetails != value) { _selectedPatientDetails = value; OnPropertyChanged(nameof(SelectedPatientDetails)); } } }
        public IEnumerable<PatientViewModel> FilteredPatients => string.IsNullOrWhiteSpace(PatientSearchText) ? Patients.OrderBy(p => p.Name) : Patients.Where(p => p.Name.Contains(PatientSearchText, StringComparison.OrdinalIgnoreCase) || (p.PhoneNumber?.Contains(PatientSearchText, StringComparison.OrdinalIgnoreCase) == true)).OrderBy(p => p.Name);

        #endregion Patient Properties

        #region Lab Properties

        public string LabResultValue
        { get => _labResultValue; set { if (_labResultValue != value) { _labResultValue = value; OnPropertyChanged(nameof(LabResultValue)); } } }
        public TestCatalogDto? SelectedTest
        { get => _selectedTest; set { if (_selectedTest != value) { _selectedTest = value; OnPropertyChanged(nameof(SelectedTest)); } } }
        public List<TestCatalogDto> AvailableTests
        { get => _availableTests; set { _availableTests = value; OnPropertyChanged(nameof(AvailableTests)); } }

        #endregion Lab Properties

        #region Visit Properties

        public string VisitHeaderText
        { get => _visitHeaderText; set { _visitHeaderText = value; OnPropertyChanged(); } }
        public string Diagnosis
        { get => _diagnosis; set { if (_diagnosis != value) { _diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); OnPropertyChanged(nameof(CanSaveVisit)); } } }
        public string Notes
        { get => _notes; set { if (_notes != value) { _notes = value; OnPropertyChanged(nameof(Notes)); } } }
        public decimal Temperature
        { get => _temperature; set { if (_temperature != value) { _temperature = value; OnPropertyChanged(nameof(Temperature)); } } }
        public int BPSystolic
        { get => _bpSystolic; set { if (_bpSystolic != value) { _bpSystolic = value; OnPropertyChanged(nameof(BPSystolic)); } } }
        public int BPDiastolic
        { get => _bpDiastolic; set { if (_bpDiastolic != value) { _bpDiastolic = value; OnPropertyChanged(nameof(BPDiastolic)); } } }
        public int Gravida
        { get => _gravida; set { if (_gravida != value) { _gravida = value; OnPropertyChanged(nameof(Gravida)); } } }
        public int Para
        { get => _para; set { if (_para != value) { _para = value; OnPropertyChanged(nameof(Para)); } } }
        public int Abortion
        { get => _abortion; set { if (_abortion != value) { _abortion = value; OnPropertyChanged(nameof(Abortion)); } } }

        #endregion Visit Properties

        #endregion Public Properties

        #region Constructor and Initialization

        public MainWindowViewModel(
            IUserService userService,
            IVisitService visitService,
            IPatientService patientService,
            IConnectionService connectionService,
            ILogger<MainWindowViewModel> logger,
            IAppSettingsService settings)
        {
            _userService = userService;
            _patientService = patientService;
            _visitService = visitService;
            _connectionService = connectionService;
            _logger = logger;
            _settings = settings;

            _authToken = "";
            ApiUrl = _settings.ApiBaseUrl ?? "http://localhost:5258";
            _logger.LogInformation("MainWindowViewModel initialized");

            // COMMENTED OUT: InitializeAsync was causing UI thread to block
            // _ = InitializeAsync();
            _logger.LogInformation("⚠️ InitializeAsync() temporarily disabled to fix window rendering issue");
        }

        private async Task InitializeAsync()
        {
            try
            {
                await LoadSettings();
                LoadPausedVisits();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during initialization");
            }
        }

        #endregion Constructor and Initialization

        #region Login Operations

        /// <summary>
        /// Sets the authentication token from external login (e.g., separate LoginWindow)
        /// </summary>
        public async Task SetAuthTokenAndInitializeAsync(string authToken)
        {
            try
            {
                _logger.LogInformation("=== SetAuthTokenAndInitializeAsync CALLED ===");
                _logger.LogInformation("   Auth Token Length: {Length}", authToken?.Length ?? 0);
                
                _authToken = authToken;
                StatusMessage = "Loading patients...";
                
                _logger.LogInformation("⏳ Loading all patients...");
                await LoadAllPatientsAsync();
                
                _logger.LogInformation("✅ Auth token set and patients loaded");
                StatusMessage = "Ready";
                
                // Trigger LoginCompleted event
                if (LoginCompleted != null)
                {
                    await LoginCompleted.Invoke();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error setting auth token and initializing");
                StatusMessage = "Error loading patients";
                OnShowErrorMessage?.Invoke("Initialization Error", ex.Message);
            }
        }

        public async Task LoginAsync(string username, string password, string? apiUrl = null)
        {
            try
            {
                StatusMessage = "Logging in...";
                _logger.LogInformation("Login started for user {Username}", username);

                var url = apiUrl ?? _settings.ApiBaseUrl;
                if (string.IsNullOrWhiteSpace(url))
                {
                    throw new InvalidOperationException("API URL is not configured");
                }

                url = url.TrimEnd('/');

                _authToken = await _userService.LoginAsync(username, password, url);
                StatusMessage = "Login successful";
                _logger.LogInformation("Login successful for user {Username}", username);

                await LoadAllPatientsAsync();
                LoadPausedVisits();

                LoginCompleted?.Invoke();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error during login");
                StatusMessage = "Login failed";

                var errorMessage = $"Login failed: {httpEx.Message}";
                if (httpEx.StatusCode == HttpStatusCode.NotFound)
                {
                    errorMessage += "\n\nThe login endpoint was not found. Check if the API is running.";
                }

                OnShowErrorMessage?.Invoke("Login Error", errorMessage);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {Username}", username);
                StatusMessage = "Login failed";
                OnShowErrorMessage?.Invoke("Login Error", ex.Message);
                throw;
            }
        }

        #endregion Login Operations

        #region Patient Management Operations

        public async Task AddNewPatientAsync(PatientCreateDto patientDto)
        {
            try
            {
                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.LogWarning("AddNewPatientAsync called without authentication");
                    this.ShowError("Please login first", "Error");
                    return;
                }

                _logger.LogInformation("Adding new patient: {PatientName}", patientDto.Name);

                await _patientService.CreatePatientAsync(patientDto);
                await LoadAllPatientsAsync();

                _logger.LogInformation("Patient added successfully: {PatientName}", patientDto.Name);
                this.ShowSuccess("Patient added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding patient: {PatientName}", patientDto?.Name);
                this.ShowError(ex.Message, "Add Patient Error");
            }
        }

        public async Task LoadAllPatientsAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_authToken))
                {
                    StatusMessage = "Please login first";
                    return;
                }

                StatusMessage = "Loading patients...";

                // Use the Core PatientService directly
                var patients = await _patientService.GetAllPatientsListAsync();
                var viewModels = PatientMapper.ToViewModels(patients)
                                              .OrderBy(p => p.Name);

                Patients.Clear();
                foreach (var vm in viewModels)
                {
                    Patients.Add(vm);
                }

                // Notify that FilteredPatients has changed
                OnPropertyChanged(nameof(FilteredPatients));

                StatusMessage = $"Loaded {Patients.Count} patients";
                PatientsLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                StatusMessage = "Error loading patients";
                _logger.LogError(ex, "Error loading patients");
                OnShowErrorMessage?.Invoke("Load Patients Error", ex.Message);
            }
        }

        public async Task SelectPatientAsync(PatientViewModel? patient)
        {
            _logger.LogInformation("➡️ SelectPatientAsync ENTERED. PatientId={PatientId}", patient?.PatientId);

            if (patient == null)
            {
                ClearPatientSelection();
                return;
            }

            // REMOVED: Don't return early if patient already selected
            // We want to allow starting a new visit for the same patient
            // if (SelectedPatient == patient)
            //     return;

            SelectedPatient = patient;

            try
            {
                IsPatientExpanderOpen = false;
                IsVisitExpanderOpen = true;

                VisitHeaderText = $"Visit – {patient.DisplayName}";

                UpdateSelectedPatientInfo(patient);

                await LoadPatientHistoryAsync(patient.PatientId);
                _logger.LogInformation("▶️ About to call StartVisitIfNotAlreadyStarted. CurrentVisitId={VisitId}", _currentVisitId);

                await StartVisitIfNotAlreadyStarted(patient);

                PatientSelected?.Invoke();
            }
            catch (Exception ex)
            {
                HandlePatientSelectionError(ex, patient);
            }
            finally
            {
                _logger.LogInformation(
                    "⬅️ SelectPatientAsync EXIT. PatientId={PatientId}",
                    patient?.PatientId);
            }
        }

        #endregion Patient Management Operations

        #region Visit Operations

        #region Lab Methods

        public async Task AddLabResultAsync()
        {
            if (SelectedTest == null)
            {
                ShowError("Please select a test first.", "Error");
                return;
            }

            if (string.IsNullOrWhiteSpace(LabResultValue))
            {
                ShowError("Please enter a result value.", "Error");
                return;
            }

            if (_currentVisitId == 0 || SelectedPatient == null)
            {
                ShowError("No active visit. Please start a visit first.", "Error");
                return;
            }

            if (string.IsNullOrEmpty(_authToken))
            {
                ShowError("Please login first.", "Authentication Required");
                return;
            }

            try
            {
                var dto = new LabResultCreateDto
                {
                    TestId = SelectedTest.TestId,
                    VisitId = _currentVisitId,
                    ResultValue = LabResultValue
                };

                await _userService.SaveLabResultAsync(dto, _settings.ApiBaseUrl, _authToken);

                // Clear after successful save
                LabResultValue = string.Empty;
                SelectedTest = null;

                ShowSuccess("Lab result saved successfully.");
                _logger.LogInformation("Lab result saved for visit {VisitId}", _currentVisitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lab result for visit {VisitId}", _currentVisitId);
                ShowError($"Error saving lab result: {ex.Message}", "Save Error");
            }
        }

        private async Task LoadAvailableTestsAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_authToken))
                {
                    AvailableTests = await _userService.GetTestCatalogAsync(_settings.ApiBaseUrl, _authToken);
                    _logger.LogInformation("Loaded {Count} available tests", AvailableTests.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading test catalog");
            }
        }

        #endregion Lab Methods

        # region Visit Methods

        private async Task StartVisitIfNotAlreadyStarted(PatientViewModel patient)
        {
            _logger.LogInformation(
                "➡️ StartVisitIfNotAlreadyStarted ENTERED. PatientId={PatientId}, CurrentVisitId={VisitId}, VisitStarting={VisitStarting}",
                patient.PatientId, _currentVisitId, _visitStarting);

            if (_currentVisitId > 0)
            {
                _logger.LogInformation(
                    "⏭ Visit already active. VisitId={VisitId}",
                    _currentVisitId);
                return;
            }

            if (_visitStarting)
            {
                _logger.LogWarning("⏭ Visit start already in progress");
                return;
            }

            _visitStarting = true;

            try
            {
                _logger.LogInformation("🚀 Attempting to start visit");

                await StartVisitForPatientAsync(patient, _logger);

                _logger.LogInformation(
                    "✅ Visit started successfully. New VisitId={VisitId}",
                    _currentVisitId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to start visit");
                
                // Show error to user
                OnShowErrorMessage?.Invoke("Visit Start Error",
                    $"Failed to start visit.\n\n{ex.Message}\n\nPlease check that the WebApi is running.");
                    
                throw;
            }
            finally
            {
                _visitStarting = false;
                _logger.LogInformation("⬅️ StartVisitIfNotAlreadyStarted EXIT");
            }
        }

        public async Task StartVisitForPatientAsync(PatientViewModel patient, ILogger<MainWindowViewModel> logger)
        {
            _logger.LogInformation(
                "➡️ StartVisitForPatientAsync ENTERED. PatientId={PatientId}",
                patient?.PatientId);

            try
            {
                // VALIDATION
                if (patient == null)
                {
                    ShowError("No patient selected.", "Error");
                    return;
                }

                if (string.IsNullOrWhiteSpace(_authToken))
                {
                    ShowError("Please login first.", "Authentication Required");
                    return;
                }

                // CREATE NEW VISIT
                _logger.LogInformation(
                    "🆕 No paused visit found. Creating new visit.");

                StatusMessage = $"Starting visit for {patient.Name}...";

                var visitRequest = new VisitSaveRequest
                {
                    PatientId = patient.PatientId,
                    Diagnosis = "Initial visit started",
                    SaveType = VisitSaveType.New,
                    AuthToken = _authToken,
                    Temperature = Temperature,
                    BloodPressureSystolic = BPSystolic,
                    BloodPressureDiastolic = BPDiastolic,
                    Gravida = Gravida,
                    Para = Para,
                    Abortion = Abortion,
                    Notes = "Visit initiated"
                };

                var result = await _visitService.SaveVisitAsync(visitRequest);

                if (!result.Success || result.VisitId == null)
                    throw new InvalidOperationException(
                        result.Message ?? "Visit creation failed.");

                _currentVisitId = result.VisitId.Value;

                VisitHeaderText = $"Visit – {patient.DisplayName}";
                StatusMessage = $"Visit #{_currentVisitId} started";

                await LoadAvailableTestsAsync();

                ShowSuccess($"Visit #{_currentVisitId} started successfully");

                _logger.LogInformation(
                    "✅ New visit created. VisitId={VisitId}",
                    _currentVisitId);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error starting visit";

                _logger.LogError(
                    ex,
                    "❌ Error starting visit for PatientId={PatientId}",
                    patient?.PatientId);

                ShowError($"Error starting visit: {ex.Message}", "Visit Error");
            }
        }

        public async Task SaveVisitAsync()
        {
            _logger.LogInformation("=== SaveVisitAsync CALLED ===");
            _logger.LogInformation("   CurrentVisitId: {VisitId}", _currentVisitId);
            _logger.LogInformation("   SelectedPatient: {Patient}", SelectedPatient?.Name ?? "NULL");
            _logger.LogInformation("   Diagnosis: '{Diagnosis}'", Diagnosis);
            _logger.LogInformation("   AuthToken: {HasToken}", !string.IsNullOrEmpty(_authToken));
            
            try
            {
                if (_currentVisitId == 0 || SelectedPatient == null)
                {
                    _logger.LogWarning("❌ Validation failed: CurrentVisitId={VisitId}, Patient={Patient}", 
                        _currentVisitId, SelectedPatient?.Name ?? "NULL");
                    ShowError("No active visit to save.", "Error");
                    return;
                }

                if (string.IsNullOrWhiteSpace(Diagnosis))
                {
                    _logger.LogWarning("❌ Validation failed: Diagnosis is empty");
                    ShowError("Diagnosis is required.", "Validation Error");
                    return;
                }

                if (string.IsNullOrEmpty(_authToken))
                {
                    _logger.LogWarning("❌ Validation failed: No auth token");
                    ShowError("Please login first.", "Authentication Required");
                    return;
                }

                _logger.LogInformation("✅ All validations passed, proceeding to save...");
                StatusMessage = "Saving visit...";

                // Create VisitSaveRequest (use your existing DTO)
                var visitRequest = new VisitSaveRequest
                {
                    VisitId = _currentVisitId,  // Required for edit
                    PatientId = SelectedPatient.PatientId,
                    Diagnosis = Diagnosis,
                    Notes = Notes,
                    Temperature = Temperature,
                    BloodPressureSystolic = BPSystolic,
                    BloodPressureDiastolic = BPDiastolic,
                    Gravida = Gravida,
                    Para = Para,
                    Abortion = Abortion,
                    SaveType = VisitSaveType.Edit,  // Editing existing visit
                    AuthToken = _authToken
                };

                // Update the visit in database using your actual service
                var result = await _visitService.SaveVisitAsync(visitRequest);  // This method exists

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.Message);
                }

                StatusMessage = $"Visit #{_currentVisitId} saved successfully";
                _logger.LogInformation("Visit {VisitId} saved for patient {PatientId}",
                    _currentVisitId, SelectedPatient.PatientId);

                // Clear form after saving
                ClearVisitForm();
                ShowSuccess($"Visit #{_currentVisitId} saved successfully");
            }
            catch (Exception ex)
            {
                StatusMessage = "Error saving visit";
                _logger.LogError(ex, "Error saving visit {VisitId}", _currentVisitId);
                ShowError($"Error saving visit: {ex.Message}", "Save Error");
            }

            SaveVisitRequested?.Invoke();
        }

        private void ClearVisitForm()
        {
            Diagnosis = string.Empty;
            Notes = string.Empty;
            Temperature = 0;
            BPSystolic = 0;
            BPDiastolic = 0;
            Gravida = 0;
            Para = 0;
            Abortion = 0;
            LabResultValue = string.Empty;
            SelectedTest = null;

            // Reset visit ID after saving
            _currentVisitId = 0;
            StatusMessage = "Ready";
        }

        public void PrintVisitSummary(PatientViewModel? selectedPatient)
        {
            if (selectedPatient == null)
            {
                ShowError("Please select a patient first.", "No Patient Selected");
                return;
            }

            if (string.IsNullOrWhiteSpace(Diagnosis))
            {
                ShowWarning("Diagnosis field is empty.", "Warning");
            }

            try
            {
                var summary = BuildVisitSummary(selectedPatient);
                SaveSummaryToFile(selectedPatient, summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing visit summary");
                ShowError($"Error creating summary: {ex.Message}", "Print Error");
            }
        }

        # endregion Visit Methods

        #endregion Visit Operations

        #region Settings and Configuration

        private async Task LoadSettings()
        {
            try
            {
                // Store old values to check if they changed
                var oldUsername = Username;
                var oldPassword = Password;
                var oldApiUrl = ApiUrl;

                // Load new values
                Username = _settings.DefaultUser;
                Password = _settings.DefaultPassword;
                ApiUrl = _settings.ApiBaseUrl ?? ApiUrl;

                // Always notify - the UI needs to refresh regardless
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Password));
                OnPropertyChanged(nameof(ApiUrl));

                _logger.LogInformation("Settings loaded - Username: {Username}, ApiUrl: {ApiUrl}",
                    Username, ApiUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
            }
        }

        public async Task OpenSettingsAsync()
        {
            try
            {
                var settingsWindow = App.Services.GetRequiredService<SettingsWindow>();
                settingsWindow.Owner = Application.Current.MainWindow;

                if (settingsWindow.ShowDialog() == true)
                {
                    await LoadSettings();
                    await ReloadIfAuthenticated();

                    OnPropertyChanged(nameof(Username));
                    OnPropertyChanged(nameof(Password));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings");
                OnShowErrorMessage?.Invoke("Settings Error", ex.Message);
            }
        }

        #endregion Settings and Configuration

        #region Helper Methods

        #region Patient Helpers

        private void ClearPatientSelection()
        {
            SelectedPatient = null;
            SelectedPatientInfo = string.Empty;
            SelectedPatientDetails = string.Empty;
            PatientHistory = "No patient selected";
        }

        private void UpdateSelectedPatientInfo(PatientViewModel patient)
        {
            SelectedPatient = patient;
            SelectedPatientInfo = patient.Name;
            SelectedPatientDetails = $"Age: {patient.Age}, Sex: {patient.SexDisplay}";
        }

        private async Task LoadPatientHistoryAsync(int patientId)
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                PatientHistory = "Please login to view history";
                return;
            }

            try
            {
                var visits = await _visitService.GetVisitHistoryForPatientAsync(patientId);
                var mapper = App.Services.GetRequiredService<IVisitMapper>();

                PatientHistory = visits?.Count > 0
                    ? string.Join("\n\n", visits.Select(v => mapper.ToDisplayString(v)))
                    : "No visit history.";
            }
            catch (Exception ex)
            {
                PatientHistory = "Error loading history";
                _logger.LogError(ex, "Error loading patient history");
            }
        }

        private void HandlePatientSelectionError(Exception ex, PatientViewModel patient)
        {
            PatientHistory = "Failed to load visit history.";
            _logger.LogError(ex, "Error selecting patient {PatientId}", patient.PatientId);
            
            // Show error to user
            OnShowErrorMessage?.Invoke("Visit Start Error", 
                $"Failed to start visit for {patient.Name}.\n\n{ex.Message}\n\nPlease check that the WebApi is running and accessible.");
        }

        #endregion Patient Helpers

        #region Auth Helpers

        private void ValidateAuthToken()
        {
            if (string.IsNullOrEmpty(_authToken))
            {
                OnShowErrorMessage?.Invoke("Error", "Please login first");
                throw new UnauthorizedAccessException("User is not authenticated");
            }
        }

        private async Task ReloadIfAuthenticated()
        {
            if (!string.IsNullOrEmpty(_authToken))
                await LoadAllPatientsAsync();
        }

        #endregion Auth Helpers

        #region Error Helpers

        private void ShowError(string message, string title = "Error")
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowErrorMessage?.Invoke(title, message));
        }

        private void ShowSuccess(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
                OnShowSuccessMessage?.Invoke(message));
        }

        private void ShowWarning(string message, string title = "Warning")
        {
            // You might want to add an OnShowWarningMessage event
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning));
        }

        private void ShowInfo(string message, string title = "Information")
        {
            // You might want to add an OnShowInfoMessage event
            Application.Current.Dispatcher.Invoke(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information));
        }

        #endregion Error Helpers

        #region Visit Helpers

        #region Visit Summary

        private string BuildVisitSummary(PatientViewModel patient)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Patient: {patient.Name}");
            sb.AppendLine($"DOB: {patient.DateOfBirth:dd MMM yyyy}");
            sb.AppendLine($"Age: {patient.Age}");
            sb.AppendLine($"Sex: {patient.Sex}");
            sb.AppendLine();
            sb.AppendLine($"Diagnosis:");
            sb.AppendLine(Diagnosis);
            sb.AppendLine();
            sb.AppendLine($"Notes:");
            sb.AppendLine(Notes);

            // Add lab results if available
            if (!string.IsNullOrWhiteSpace(LabResultValue) && SelectedTest != null)
            {
                sb.AppendLine();
                sb.AppendLine($"Lab Result:");
                sb.AppendLine($"{SelectedTest.TestName}: {LabResultValue}");
            }

            return sb.ToString();
        }

        private void SaveSummaryToFile(PatientViewModel patient, string summary)
        {
            const string TEMP_FOLDER = @"C:\temp";
            Directory.CreateDirectory(TEMP_FOLDER);

            // Sanitize filename
            var fileName = $"{patient.Name.Replace(" ", "_")}_VisitSummary_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var invalidChars = Path.GetInvalidFileNameChars();
            foreach (var c in invalidChars)
            {
                fileName = fileName.Replace(c.ToString(), "");
            }

            var path = Path.Combine(TEMP_FOLDER, fileName);
            System.IO.File.WriteAllText(path, summary);

            ShowInfo($"Summary saved to:\n{path}", "Summary Saved");
        }

        #endregion Visit Summary

        #region Paused Visits Helpers

        public async void LoadPausedVisits()
        {
            try
            {
                _logger.LogInformation("Loading paused visits...");
                
                // Properly await the async method instead of blocking
                var pausedVisits = await _visitService.GetPausedVisitsTodayAsync();

                _logger.LogInformation("Loaded {Count} paused visits", pausedVisits?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading paused visits");
            }
        }

        #endregion Paused Visits Helpers

        #endregion Visit Helpers

        #region INotifyPropertyChanged Implementation

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion INotifyPropertyChanged Implementation

        #endregion Helper Methods
    }
}