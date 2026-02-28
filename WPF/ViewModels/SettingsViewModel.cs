using Core.AI;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using WPF.Configuration;
using WPF.Services;

namespace WPF.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        #region Dependencies

        private readonly IAppSettingsService _appSettings;
        private readonly IConnectionService  _connectionService;
        private readonly IUserService        _userService;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IAiService          _aiService;

        // ── UI-decoupling events (subscribed by SettingsWindow) ─────────────────
        public event Action<string, string>? OnShowInfo;
        public event Action<string, string>? OnShowError;
        public event Action<string, string>? OnShowWarning;
        /// <summary>Returns true if user clicked Yes.</summary>
        public event Func<string, string, bool>? OnConfirmDialog;

        // Captured on construction (UI thread) so background callbacks can marshal
        private readonly SynchronizationContext? _syncCtx = SynchronizationContext.Current;
        private void RunOnUi(Action a) { if (_syncCtx != null) _syncCtx.Post(_ => a(), null); else a(); }

        // Shared HttpClient — reuse across catalog operations to avoid socket exhaustion
        private static readonly System.Net.Http.HttpClient _httpClient = new();

        #endregion

        #region Constructor

        public SettingsViewModel(
            IAppSettingsService appSettings,
            IConnectionService connectionService,
            IUserService userService,
            ILogger<SettingsViewModel> logger,
            IAiService aiService)
        {
            _appSettings       = appSettings;
            _connectionService = connectionService;
            _userService       = userService;
            _logger            = logger;
            _aiService         = aiService;

            LoadCurrentSettings();
            BuildStaticLists();

            _connectionService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _logger.LogInformation("SettingsViewModel initialized");
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 1 — CONNECTION
        // ════════════════════════════════════════════════════════════════════════
        #region Connection Tab

        private string _apiBaseUrl          = string.Empty;
        private string? _defaultUser;
        private string? _defaultPassword;
        private string? _defaultUserName;
        private bool _enableDetailedErrors;
        private int  _httpTimeoutSeconds    = 30;
        private bool _testConnectionAfterSave = true;
        private bool _isTestingConnection;
        private string _connectionStatus      = "Not tested";
        private string _connectionStatusColor = "Gray";

        public string ApiBaseUrl
        {
            get => _apiBaseUrl;
            set { _apiBaseUrl = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanTestConnection)); }
        }
        public string? DefaultUser
        {
            get => _defaultUser;
            set { _defaultUser = value; OnPropertyChanged(); }
        }
        public string? DefaultPassword
        {
            get => _defaultPassword;
            set { _defaultPassword = value; OnPropertyChanged(); }
        }
        public string? DefaultUserName
        {
            get => _defaultUserName;
            set { _defaultUserName = value; OnPropertyChanged(); }
        }
        public bool EnableDetailedErrors
        {
            get => _enableDetailedErrors;
            set { _enableDetailedErrors = value; OnPropertyChanged(); }
        }
        public int HttpTimeoutSeconds
        {
            get => _httpTimeoutSeconds;
            set { _httpTimeoutSeconds = value; OnPropertyChanged(); }
        }
        public bool TestConnectionAfterSave
        {
            get => _testConnectionAfterSave;
            set { _testConnectionAfterSave = value; OnPropertyChanged(); }
        }
        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            private set { _isTestingConnection = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanTestConnection)); }
        }
        public string ConnectionStatus
        {
            get => _connectionStatus;
            private set { _connectionStatus = value; OnPropertyChanged(); }
        }
        public string ConnectionStatusColor
        {
            get => _connectionStatusColor;
            private set { _connectionStatusColor = value; OnPropertyChanged(); }
        }
        public bool CanTestConnection => !string.IsNullOrWhiteSpace(ApiBaseUrl) && !IsTestingConnection;

        public async Task TestConnectionAsync()
        {
            if (!CanTestConnection) return;
            IsTestingConnection  = true;
            ConnectionStatus     = "Testing connection...";
            ConnectionStatusColor = "Yellow";

            try
            {
                var result = await _connectionService.TestApiConnectionAsync(ApiBaseUrl);
                if (result.Success)
                {
                    ConnectionStatus      = "✓ Connection successful!";
                    ConnectionStatusColor = "Green";
                    OnShowInfo?.Invoke("Connection Successful", $"Connected to:\n{ApiBaseUrl}\n\nResponse: {result.ResponseTime.TotalMilliseconds:F0} ms");



                }
                else
                {
                    ConnectionStatus      = $"✗ {result.Message}";
                    ConnectionStatusColor = "Red";
                    OnShowWarning?.Invoke("Connection Failed", $"Connection failed:\n{result.Message}");
                }
            }
            catch (Exception ex)
            {
                ConnectionStatus      = "✗ Connection test error";
                ConnectionStatusColor = "Red";
                _logger.LogError(ex, "Error testing connection");
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        public async Task<bool> TestConnectionAfterSaveAsync()
        {
            if (!TestConnectionAfterSave) return true;
            try
            {
                var result = await _connectionService.TestApiConnectionAsync(ApiBaseUrl);
                if (!result.Success)
                    OnShowWarning?.Invoke("Connection Warning", $"Settings saved but connection failed:\n{result.Message}");
                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection after save");
                return false;
            }
        }

        private bool _isDisposed;

        private void OnConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            if (_isDisposed) return;
            RunOnUi(() => {
                if (_isDisposed) return;
                ConnectionStatus      = e.IsConnected ? "✓ Connected" : "✗ Disconnected";
                ConnectionStatusColor = e.IsConnected ? "Green" : "Red";
            });
        }

        public void Cleanup()
        {
            _isDisposed = true;
            _connectionService.ConnectionStatusChanged -= OnConnectionStatusChanged;
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 2 — LAB CATALOG
        // ════════════════════════════════════════════════════════════════════════
        #region Lab Tab

        private ObservableCollection<TestCatalogDto> _tests = new();
        private TestCatalogDto? _selectedLabTest;
        private bool _isLoadingLab;

        // ── New test form ──
        private string _newTestName  = string.Empty;
        private string _newTestUnit  = string.Empty;
        private string _newTestRange = string.Empty;

        public ObservableCollection<TestCatalogDto> Tests
        {
            get => _tests;
            set { _tests = value; OnPropertyChanged(); }
        }
        public TestCatalogDto? SelectedLabTest
        {
            get => _selectedLabTest;
            set { _selectedLabTest = value; OnPropertyChanged(); }
        }
        public bool IsLoadingLab
        {
            get => _isLoadingLab;
            private set { _isLoadingLab = value; OnPropertyChanged(); }
        }
        public string NewTestName
        {
            get => _newTestName;
            set { _newTestName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddTest)); }
        }
        public string NewTestUnit
        {
            get => _newTestUnit;
            set { _newTestUnit = value; OnPropertyChanged(); }
        }
        public string NewTestRange
        {
            get => _newTestRange;
            set { _newTestRange = value; OnPropertyChanged(); }
        }
        public bool CanAddTest => !string.IsNullOrWhiteSpace(NewTestName);

        public async Task LoadTestsAsync()
        {
            if (string.IsNullOrEmpty(_authToken)) return;
            IsLoadingLab = true;
            try
            {
                var list = await _userService.GetTestCatalogAsync(ApiBaseUrl, _authToken);
                Tests.Clear();
                foreach (var t in list) Tests.Add(t);
            }
            catch (Exception ex) { _logger.LogError(ex, "Error loading test catalog"); }
            finally { IsLoadingLab = false; }
        }

        public async Task AddTestAsync()
        {
            if (!CanAddTest || string.IsNullOrEmpty(_authToken)) return;
            try
            {
                // POST via IUserService → ApiService
                var dto = new TestCatalogCreateDto
                {
                    TestName   = NewTestName.Trim(),
                    TestUnit   = string.IsNullOrWhiteSpace(NewTestUnit)  ? null : NewTestUnit.Trim(),
                    NormalRange = string.IsNullOrWhiteSpace(NewTestRange) ? null : NewTestRange.Trim()
                };
                await PostTestCatalogAsync(dto);

                NewTestName  = string.Empty;
                NewTestUnit  = string.Empty;
                NewTestRange = string.Empty;
                await LoadTestsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding test");
                OnShowError?.Invoke("Error", $"Error adding test:\n{ex.Message}");

            }
        }

        public async Task DeleteTestAsync()
        {
            if (SelectedLabTest == null || string.IsNullOrEmpty(_authToken)) return;
            if (OnConfirmDialog?.Invoke("Confirm", $"Delete test \"{SelectedLabTest.TestName}\"?") != true) return;




            try
            {
                await DeleteTestCatalogAsync(SelectedLabTest.TestId);
                await LoadTestsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test");
                OnShowError?.Invoke("Error", $"Error deleting test:\n{ex.Message}");

            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 3 — PHARMACY CATALOG
        // ════════════════════════════════════════════════════════════════════════
        #region Pharmacy Tab

        private ObservableCollection<DrugCatalogDto> _drugs = new();
        private DrugCatalogDto? _selectedDrug;
        private bool _isLoadingPharmacy;

        // ── New drug form ──
        private string _newDrugName        = string.Empty;
        private string _newDrugComposition = string.Empty;
        private string _newDrugForm        = string.Empty;
        private string _newDrugStrength    = string.Empty;
        private string _drugFormFilter     = string.Empty;

        public ObservableCollection<DrugCatalogDto> Drugs
        {
            get => _drugs;
            set { _drugs = value; OnPropertyChanged(); }
        }
        public DrugCatalogDto? SelectedDrug
        {
            get => _selectedDrug;
            set { _selectedDrug = value; OnPropertyChanged(); }
        }
        public bool IsLoadingPharmacy
        {
            get => _isLoadingPharmacy;
            private set { _isLoadingPharmacy = value; OnPropertyChanged(); }
        }
        public string NewDrugName
        {
            get => _newDrugName;
            set { _newDrugName = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanAddDrug)); }
        }
        public string NewDrugComposition
        {
            get => _newDrugComposition;
            set { _newDrugComposition = value; OnPropertyChanged(); }
        }
        public string NewDrugForm
        {
            get => _newDrugForm;
            set { _newDrugForm = value; OnPropertyChanged(); }
        }
        public string NewDrugStrength
        {
            get => _newDrugStrength;
            set { _newDrugStrength = value; OnPropertyChanged(); }
        }
        public string DrugFormFilter
        {
            get => _drugFormFilter;
            set { _drugFormFilter = value; OnPropertyChanged(); OnPropertyChanged(nameof(FilteredDrugs)); }
        }
        public bool CanAddDrug => !string.IsNullOrWhiteSpace(NewDrugName);

        public IEnumerable<DrugCatalogDto> FilteredDrugs =>
            string.IsNullOrWhiteSpace(DrugFormFilter)
                ? _drugs
                : _drugs.Where(d =>
                    (d.BrandName?.Contains(DrugFormFilter, StringComparison.OrdinalIgnoreCase) == true) ||
                    (d.Form?.Contains(DrugFormFilter, StringComparison.OrdinalIgnoreCase) == true) ||
                    (d.Composition?.Contains(DrugFormFilter, StringComparison.OrdinalIgnoreCase) == true));

        public static IReadOnlyList<string> DrugForms { get; } =
            new[] { "Tablet", "Capsule", "Syrup", "Injection", "Cream", "Drops", "Inhaler", "Suppository", "Patch", "Other" };

        public async Task LoadDrugsAsync()
        {
            if (string.IsNullOrEmpty(_authToken)) return;
            IsLoadingPharmacy = true;
            try
            {
                var list = await _userService.GetDrugCatalogAsync(ApiBaseUrl, _authToken);
                Drugs.Clear();
                foreach (var d in list) Drugs.Add(d);
                OnPropertyChanged(nameof(FilteredDrugs));
            }
            catch (Exception ex) { _logger.LogError(ex, "Error loading drug catalog"); }
            finally { IsLoadingPharmacy = false; }
        }

        public async Task AddDrugAsync()
        {
            if (!CanAddDrug || string.IsNullOrEmpty(_authToken)) return;
            try
            {
                var dto = new DrugCreateDto
                {
                    BrandName      = NewDrugName.Trim(),
                    Composition    = string.IsNullOrWhiteSpace(NewDrugComposition) ? null : NewDrugComposition.Trim(),
                    Form           = string.IsNullOrWhiteSpace(NewDrugForm)        ? null : NewDrugForm.Trim(),
                    DosageStrength = string.IsNullOrWhiteSpace(NewDrugStrength)    ? null : NewDrugStrength.Trim()
                };
                await PostDrugCatalogAsync(dto);

                NewDrugName        = string.Empty;
                NewDrugComposition = string.Empty;
                NewDrugForm        = string.Empty;
                NewDrugStrength    = string.Empty;
                await LoadDrugsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding drug");
                OnShowError?.Invoke("Error", $"Error adding drug:\n{ex.Message}");

            }
        }

        public async Task DeleteDrugAsync()
        {
            if (SelectedDrug == null || string.IsNullOrEmpty(_authToken)) return;
            if (OnConfirmDialog?.Invoke("Confirm", $"Delete drug \"{SelectedDrug.BrandName}\"?") != true) return;




            try
            {
                await DeleteDrugCatalogAsync(SelectedDrug.DrugId);
                await LoadDrugsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting drug");
                OnShowError?.Invoke("Error", $"Error deleting drug:\n{ex.Message}");

            }
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 4 — DOCTOR PROFILE
        // ════════════════════════════════════════════════════════════════════════
        #region Doctor Tab

        private string _doctorName      = string.Empty;
        private string _doctorTitle     = "Dr.";
        private string _doctorSpecialty = "General";
        private string _doctorLicense   = string.Empty;
        private string _clinicName      = string.Empty;
        private string _clinicPhone     = string.Empty;

        public string DoctorName
        {
            get => _doctorName;
            set { _doctorName = value; OnPropertyChanged(); }
        }
        public string DoctorTitle
        {
            get => _doctorTitle;
            set { _doctorTitle = value; OnPropertyChanged(); }
        }
        public string DoctorSpecialty
        {
            get => _doctorSpecialty;
            set
            {
                _doctorSpecialty = value;
                OnPropertyChanged();
                // Auto-suggest linked color when specialty changes and scheme is SpecialtyLinked
                if (ColorScheme == "SpecialtyLinked")
                {
                    PreviewAccentHex = ThemeService.GetSpecialtyAccent(value);
                    OnPropertyChanged(nameof(PreviewAccentHex));
                }
            }
        }
        public string DoctorLicense
        {
            get => _doctorLicense;
            set { _doctorLicense = value; OnPropertyChanged(); }
        }
        public string ClinicName
        {
            get => _clinicName;
            set { _clinicName = value; OnPropertyChanged(); }
        }
        public string ClinicPhone
        {
            get => _clinicPhone;
            set { _clinicPhone = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> DoctorTitles { get; } =
            new[] { "Dr.", "Prof.", "Mr.", "Ms.", "Mrs.", "Ass. Prof." };

        public static IReadOnlyList<string> Specialties { get; } =
            ClinicalSystem.All.Select(s => s.Name).OrderBy(n => n).ToList();

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 5 — APPEARANCE
        // ════════════════════════════════════════════════════════════════════════
        #region Appearance Tab

        private string _colorScheme    = "SpecialtyLinked";
        private bool   _isDarkMode     = false;
        private string _previewAccentHex = "#2196F3";

        public string ColorScheme
        {
            get => _colorScheme;
            set
            {
                _colorScheme = value;
                OnPropertyChanged();
                PreviewAccentHex = ThemeService.ResolveAccent(value, DoctorSpecialty);
                OnPropertyChanged(nameof(PreviewAccentHex));
            }
        }
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set { _isDarkMode = value; OnPropertyChanged(); }
        }
        public string PreviewAccentHex
        {
            get => _previewAccentHex;
            private set { _previewAccentHex = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> ColorSchemeNames => ThemeService.SchemeNames;

        public void PreviewTheme()
        {
            ThemeService.Apply(ColorScheme, DoctorSpecialty, IsDarkMode);
        }

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // TAB 6 — AI PROVIDER
        // ════════════════════════════════════════════════════════════════════════
    }
}
