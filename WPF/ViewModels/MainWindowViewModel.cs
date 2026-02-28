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
using WPF.Helpers;
using WPF.Views;

namespace WPF.ViewModels
{
    public partial class MainWindowViewModel : BaseViewModel
    {
        #region Services and Dependencies

        private readonly IUserService _userService;
        private readonly IPatientService _patientService;
        private readonly IVisitService _visitService;
        private readonly IConnectionService _connectionService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IAppSettingsService            _settings;
        private readonly IVisitMapper                    _visitMapper;
        private readonly Func<RegisterPatientViewModel>  _registerPatientVmFactory;

        #endregion Services and Dependencies

        #region Private Fields

        private ObservableCollection<PatientViewModel> _patients = new();
        private PatientViewModel? _selectedPatient;
        private List<TestCatalogDto> _availableTests = new();
        private TestCatalogDto? _selectedTest;
        private string _authToken = string.Empty;
        private string? _username;
        private string? _password;
        private string _apiUrl = string.Empty;
        private string _statusMessage = "Ready";
        private string _visitHeaderText = "Visit";
        private string _patientSearchText = string.Empty;
        private string _patientHistory = string.Empty;
        private string _selectedPatientInfo = string.Empty;
        private string _selectedPatientDetails = string.Empty;
        private string _diagnosis = string.Empty;
        private string _notes = string.Empty;
        private string _presentingSymptoms    = string.Empty;
        private string _historyAndExamination = string.Empty;
        private string _imagingFindings       = string.Empty;
        private string _aiSuggestions         = string.Empty;
        private List<LabAttachment> _imagingAttachments = new();
        private List<LabAttachment> _historyAttachments = new();
        private bool   _isAiThinking;
        private decimal _temperature;
        private int _currentVisitId;
        private int _lastLoadedPatientId;   // tracks which patient's history is currently shown
        private VisitPageViewModel? _currentVisit;
        private int _bpSystolic;
        private int _bpDiastolic;
        private int _gravida;
        private int _para;
        private int _abortion;
        private bool _saveButtonEnabled;
        private bool _visitStarting;
        private bool _isPatientExpanderOpen = true;
        private bool _isVisitExpanderOpen;

        // ── Prescription fields ───────────────────────────────────────────────
        private List<Core.DTOs.DrugCatalogDto> _availableDrugs = new();
        private List<PrescriptionLineItem> _prescriptions = new();
        private string _rxDrugText     = string.Empty;
        private string _rxDose         = string.Empty;
        private string _rxRoute        = string.Empty;
        private string _rxFrequency    = string.Empty;
        private string _rxDuration     = string.Empty;
        private string _rxInstructions = string.Empty;

        /// <summary>
        /// Last-used prescription values per drug name (case-insensitive key).
        /// Persisted only for the lifetime of the application session.
        /// </summary>
        private readonly Dictionary<string, PrescriptionLineItem> _lastUsedRx =
            new(StringComparer.OrdinalIgnoreCase);

        // ── Lab result fields ─────────────────────────────────────────────────
        private List<LabResultLineItem> _labResults = new();
        private string _labTestSearchText  = string.Empty;
        private string _labResultValue     = string.Empty;
        private string _labResultUnit      = string.Empty;
        private string _labResultNotes     = string.Empty;
        private int    _labSelectedTestId;

        #endregion Private Fields

        #region Events

        public event Action<string, string>? OnShowErrorMessage;

        public event Action<string>? OnShowSuccessMessage;

        public event Action<string, string>? OnShowWarningMessage;
        public event Action<string, string>? OnShowInfoMessage;

        public event Func<Task>? SaveVisitRequested;

        public event Func<Task>? LoginCompleted;

        public event Func<Task>? PatientsLoaded;

        public event Func<Task>? PatientSelected;


        #endregion Events

        #region Boolean Properties

        public bool IsPatientExpanderOpen
        { get => _isPatientExpanderOpen; set { _isPatientExpanderOpen = value; OnPropertyChanged(); } }
        public bool IsVisitExpanderOpen
        { get => _isVisitExpanderOpen; set { _isVisitExpanderOpen = value; OnPropertyChanged(); } }
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

        #region Culture / Layout Properties

        public System.Windows.FlowDirection AppFlowDirection
            => CultureHelper.GetFlowDirection(_settings.Language);

        public string AppLanguageTag
            => _settings.Language;

        #endregion Culture / Layout Properties

        #region Patient Properties

        public ObservableCollection<PatientViewModel> Patients { get; } = new();
        public PatientViewModel? SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (_selectedPatient == value) return;
                _selectedPatient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedPatient));
                OnPropertyChanged(nameof(HasNoSelectedPatient));
            }
        }
        public bool HasSelectedPatient => _selectedPatient != null;
        public bool HasNoSelectedPatient => _selectedPatient == null;
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

        public TestCatalogDto? SelectedTest
        { get => _selectedTest; set { if (_selectedTest != value) { _selectedTest = value; OnPropertyChanged(nameof(SelectedTest)); } } }
        public List<TestCatalogDto> AvailableTests
        { get => _availableTests; set { _availableTests = value; OnPropertyChanged(nameof(AvailableTests)); } }

        #endregion Lab Properties

        #region Visit Properties

        public string VisitHeaderText
        { get => _visitHeaderText; set { _visitHeaderText = value; OnPropertyChanged(); } }

        public VisitPageViewModel? CurrentVisit
        {
            get => _currentVisit;
            private set => SetProperty(ref _currentVisit, value);
        }

        public string Diagnosis
        { get => _diagnosis; set { if (_diagnosis != value) { _diagnosis = value; OnPropertyChanged(nameof(Diagnosis)); OnPropertyChanged(nameof(CanSaveVisit)); } } }
        public string Notes
        { get => _notes; set { if (_notes != value) { _notes = value; OnPropertyChanged(nameof(Notes)); } } }
        public string PresentingSymptoms
        { get => _presentingSymptoms; set { if (_presentingSymptoms != value) { _presentingSymptoms = value; OnPropertyChanged(); } } }
        public string HistoryAndExamination
        { get => _historyAndExamination; set { if (_historyAndExamination != value) { _historyAndExamination = value; OnPropertyChanged(); } } }
        public string ImagingFindings
        { get => _imagingFindings; set { if (_imagingFindings != value) { _imagingFindings = value; OnPropertyChanged(); } } }
        public string AiSuggestions
        { get => _aiSuggestions; set { if (_aiSuggestions != value) { _aiSuggestions = value; OnPropertyChanged(); } } }
        public bool IsAiThinking
        { get => _isAiThinking; set { if (_isAiThinking != value) { _isAiThinking = value; OnPropertyChanged(); } } }

        // ── Section attachments ───────────────────────────────────────────────
        public List<LabAttachment> ImagingAttachments
        {
            get => _imagingAttachments;
            set { _imagingAttachments = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasImagingAttachments)); }
        }
        public bool HasImagingAttachments => _imagingAttachments.Count > 0;

        public List<LabAttachment> HistoryAttachments
        {
            get => _historyAttachments;
            set { _historyAttachments = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasHistoryAttachments)); }
        }
        public bool HasHistoryAttachments => _historyAttachments.Count > 0;
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

        #region Prescription Properties

        public List<Core.DTOs.DrugCatalogDto> AvailableDrugs
        {
            get => _availableDrugs;
            set { _availableDrugs = value; OnPropertyChanged(); }
        }

        public List<PrescriptionLineItem> Prescriptions
        {
            get => _prescriptions;
            set { _prescriptions = value; OnPropertyChanged(); }
        }

        public string RxDrugText
        {
            get => _rxDrugText;
            set { _rxDrugText = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddPrescription)); }
        }
        public string RxDose
        {
            get => _rxDose;
            set { _rxDose = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddPrescription)); }
        }
        public string RxRoute
        {
            get => _rxRoute;
            set { _rxRoute = value; OnPropertyChanged(); }
        }
        public string RxFrequency
        {
            get => _rxFrequency;
            set { _rxFrequency = value; OnPropertyChanged(); }
        }
        public string RxDuration
        {
            get => _rxDuration;
            set { _rxDuration = value; OnPropertyChanged(); }
        }
        public string RxInstructions
        {
            get => _rxInstructions;
            set { _rxInstructions = value; OnPropertyChanged(); }
        }

        public bool CanAddPrescription =>
            !string.IsNullOrWhiteSpace(RxDrugText) && !string.IsNullOrWhiteSpace(RxDose);

        public static IReadOnlyList<string> RouteOptions { get; } =
            new[] { "Oral", "IV", "IM", "SC", "Topical", "Inhaled", "Sublingual", "Rectal", "Ophthalmic", "Otic", "Nasal" };

        public static IReadOnlyList<string> FrequencyOptions { get; } =
            new[] { "Once daily", "Twice daily (BID)", "Three times daily (TID)", "Four times daily (QID)",
                    "Every 8 hours", "Every 6 hours", "Every 12 hours",
                    "At bedtime (QHS)", "As needed (PRN)", "Stat (immediately)" };

        /// <summary>Smart defaults by drug Form — auto-populates route/frequency/duration on drug selection.</summary>
        public static readonly Dictionary<string, (string Route, string Frequency, string Duration)> FormDefaults =
            new(StringComparer.OrdinalIgnoreCase)
        {
            ["Tablet"]      = ("Oral",       "Twice daily (BID)",       "7"),
            ["Capsule"]     = ("Oral",       "Twice daily (BID)",       "7"),
            ["Syrup"]       = ("Oral",       "Three times daily (TID)", "5"),
            ["Suspension"]  = ("Oral",       "Twice daily (BID)",       "7"),
            ["Injection"]   = ("IM",         "Once daily",              "3"),
            ["Cream"]       = ("Topical",    "Twice daily (BID)",       "7"),
            ["Ointment"]    = ("Topical",    "Once daily",              "7"),
            ["Drops"]       = ("Ophthalmic", "Four times daily (QID)",  "5"),
            ["Inhaler"]     = ("Inhaled",    "Twice daily (BID)",       "30"),
            ["Patch"]       = ("Topical",    "Once daily",              "7"),
            ["Suppository"] = ("Rectal",     "Once daily",              "3"),
            ["Solution"]    = ("Oral",       "Three times daily (TID)", "5"),
        };

        #endregion Prescription Properties

        #region Lab Result Properties

        public List<LabResultLineItem> LabResults
        {
            get => _labResults;
            set { _labResults = value; OnPropertyChanged(); }
        }

        public string LabTestSearchText
        {
            get => _labTestSearchText;
            set { _labTestSearchText = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddLabItem)); }
        }

        public string LabResultValue
        {
            get => _labResultValue;
            set { _labResultValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddLabItem)); }
        }

        public string LabResultUnit
        {
            get => _labResultUnit;
            set { _labResultUnit = value; OnPropertyChanged(); }
        }

        public string LabResultNotes
        {
            get => _labResultNotes;
            set { _labResultNotes = value; OnPropertyChanged(); }
        }

        public int LabSelectedTestId
        {
            get => _labSelectedTestId;
            set { _labSelectedTestId = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddLabItem)); }
        }

        /// <summary>Unit options populated when a test is selected from autocomplete.</summary>
        public List<Core.DTOs.LabUnitOption> LabUnitOptions
        {
            get => _labUnitOptions;
            set
            {
                _labUnitOptions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LabNormalRangeOptions));
            }
        }
        private List<Core.DTOs.LabUnitOption> _labUnitOptions = new();

        /// <summary>
        /// All available normal-range strings for the current test,
        /// shown as dropdown suggestions in the Normal Range ComboBox.
        /// Each entry is formatted as "range  (unit)" so the doctor
        /// can quickly identify which kit standard each range belongs to.
        /// </summary>
        public List<string> LabNormalRangeOptions =>
            _labUnitOptions
                .Where(o => !string.IsNullOrWhiteSpace(o.NormalRange))
                .Select(o => string.IsNullOrWhiteSpace(o.Unit)
                    ? o.NormalRange
                    : $"{o.NormalRange}  ({o.Unit})")
                .ToList();

        /// <summary>The currently chosen unit+range option (from dropdown or custom).</summary>
        public Core.DTOs.LabUnitOption? LabSelectedUnitOption
        {
            get => _labSelectedUnitOption;
            set
            {
                _labSelectedUnitOption = value;
                OnPropertyChanged();
                if (value != null)
                {
                    LabResultUnit   = value.Unit;
                    LabNormalRange  = value.NormalRange;
                }
            }
        }
        private Core.DTOs.LabUnitOption? _labSelectedUnitOption;

        /// <summary>Editable normal range — pre-filled from catalog but overridable per-visit.</summary>
        public string LabNormalRange
        {
            get => _labNormalRange;
            set { _labNormalRange = value; OnPropertyChanged(); }
        }
        private string _labNormalRange = string.Empty;

        public bool CanAddLabItem =>
            !string.IsNullOrWhiteSpace(LabTestSearchText) &&
            !string.IsNullOrWhiteSpace(LabResultValue);

        #endregion Lab Result Properties

        #endregion Public Properties

        #region Constructor and Initialization

        public MainWindowViewModel(
            IUserService userService,
            IVisitService visitService,
            IPatientService patientService,
            IConnectionService connectionService,
            ILogger<MainWindowViewModel> logger,
            IAppSettingsService settings,
            IVisitMapper visitMapper,
            Func<RegisterPatientViewModel> registerPatientVmFactory,
            VisitPageViewModel visitPageViewModel)
        {
            _userService = userService;
            _patientService = patientService;
            _visitService = visitService;
            _connectionService = connectionService;
            _logger = logger;
            _settings              = settings;
            _visitMapper              = visitMapper;
            _registerPatientVmFactory = registerPatientVmFactory;
            CurrentVisit = visitPageViewModel;

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
                await LoadPausedVisitsAsync();
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
                _logger.LogInformation("   Auth Token Length: {Length}", authToken.Length);
                
                _authToken = authToken;
                StatusMessage = "Loading patients...";
                
                _logger.LogInformation("⏳ Loading all patients and catalogs...");
                await Task.WhenAll(
                    LoadAllPatientsAsync(),
                    LoadDrugCatalogAsync(),
                    LoadAvailableTestsAsync());

                _logger.LogInformation("✅ Auth token set, patients and catalogs loaded");
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
                await LoadPausedVisitsAsync();

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

        /// <summary>
        /// Called by the view after the Settings dialog closes with a save.
        /// Pure post-save refresh — no dialog or window logic here.
        /// </summary>
        public async Task OnSettingsSavedAsync()
        {
            try
            {
                await LoadSettings();
                await ReloadIfAuthenticated();
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Password));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying saved settings");
                OnShowErrorMessage?.Invoke("Settings Error", ex.Message);
            }
        }

        /// <summary>Returns the current auth token for the settings dialog.</summary>
        public string GetAuthToken() => _authToken;

        public RegisterPatientViewModel CreateRegisterPatientViewModel()
            => _registerPatientVmFactory();

        #endregion Settings and Configuration

    }
}
