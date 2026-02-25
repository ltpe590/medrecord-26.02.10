using Core.AI;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;
using WPF.Configuration;
using WPF.Services;

namespace WPF.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        #region Dependencies

        private readonly IAppSettingsService _appSettings;
        private readonly IConnectionService  _connectionService;
        private readonly IUserService        _userService;
        private readonly ILogger<SettingsViewModel> _logger;
        private readonly IAiService          _aiService;

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
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            $"Connected to:\n{ApiBaseUrl}\n\nResponse: {result.ResponseTime.TotalMilliseconds:F0} ms",
                            "Connection Successful", MessageBoxButton.OK, MessageBoxImage.Information));
                }
                else
                {
                    ConnectionStatus      = $"✗ {result.Message}";
                    ConnectionStatusColor = "Red";
                    Application.Current.Dispatcher.Invoke(() =>
                        MessageBox.Show($"Connection failed:\n{result.Message}",
                            "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning));
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
                    MessageBox.Show($"Settings saved but connection failed:\n{result.Message}",
                        "Connection Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            Application.Current?.Dispatcher.Invoke(() =>
            {
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
                MessageBox.Show($"Error adding test:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteTestAsync()
        {
            if (SelectedLabTest == null || string.IsNullOrEmpty(_authToken)) return;
            var confirm = MessageBox.Show(
                $"Delete test \"{SelectedLabTest.TestName}\"?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeleteTestCatalogAsync(SelectedLabTest.TestId);
                await LoadTestsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting test");
                MessageBox.Show($"Error deleting test:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Error adding drug:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task DeleteDrugAsync()
        {
            if (SelectedDrug == null || string.IsNullOrEmpty(_authToken)) return;
            var confirm = MessageBox.Show(
                $"Delete drug \"{SelectedDrug.BrandName}\"?",
                "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                await DeleteDrugCatalogAsync(SelectedDrug.DrugId);
                await LoadDrugsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting drug");
                MessageBox.Show($"Error deleting drug:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
        #region AI Tab

        // ── Provider selection ────────────────────────────────────────────────
        private string _aiProvider    = "None";
        private bool   _aiIsTestingProvider;
        private string _aiStatus      = string.Empty;
        private string _aiStatusColor = "Gray";

        public static IReadOnlyList<string> AiProviders { get; } =
            new[] { "None", "Claude", "ChatGpt", "Ollama" };

        public string AiProvider
        {
            get => _aiProvider;
            set
            {
                _aiProvider = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowClaudeFields));
                OnPropertyChanged(nameof(ShowOpenAiFields));
                OnPropertyChanged(nameof(ShowOllamaFields));
                OnPropertyChanged(nameof(CanTestAi));
                AiStatus = string.Empty;
            }
        }

        public bool ShowClaudeFields  => _aiProvider == "Claude";
        public bool ShowOpenAiFields  => _aiProvider == "ChatGpt";
        public bool ShowOllamaFields  => _aiProvider == "Ollama";

        public bool AiIsTestingProvider
        {
            get => _aiIsTestingProvider;
            private set { _aiIsTestingProvider = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanTestAi)); }
        }
        public string AiStatus
        {
            get => _aiStatus;
            private set { _aiStatus = value; OnPropertyChanged(); }
        }
        public string AiStatusColor
        {
            get => _aiStatusColor;
            private set { _aiStatusColor = value; OnPropertyChanged(); }
        }
        public bool CanTestAi => !AiIsTestingProvider && _aiProvider != "None";

        // ── Claude fields ─────────────────────────────────────────────────────
        private string _claudeApiKey = string.Empty;
        private string _claudeModel  = "claude-opus-4-5";

        public string ClaudeApiKey
        {
            get => _claudeApiKey;
            set { _claudeApiKey = value; OnPropertyChanged(); }
        }
        public string ClaudeModel
        {
            get => _claudeModel;
            set { _claudeModel = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> ClaudeModels { get; } = new[]
        {
            "claude-opus-4-6",
            "claude-sonnet-4-5",
            "claude-haiku-4-5"
        };

        // ── OpenAI / ChatGPT fields ───────────────────────────────────────────
        private string _openAiApiKey = string.Empty;
        private string _openAiModel  = "gpt-4o";

        public string OpenAiApiKey
        {
            get => _openAiApiKey;
            set { _openAiApiKey = value; OnPropertyChanged(); }
        }
        public string OpenAiModel
        {
            get => _openAiModel;
            set { _openAiModel = value; OnPropertyChanged(); }
        }

        public static IReadOnlyList<string> OpenAiModels { get; } = new[]
        {
            "gpt-4o",
            "gpt-4o-mini",
            "gpt-4-turbo",
            "gpt-3.5-turbo"
        };

        // ── Ollama fields ─────────────────────────────────────────────────────
        private string _ollamaBaseUrl = "http://localhost:11434";
        private string _ollamaModel   = string.Empty;
        private ObservableCollection<string> _ollamaModels = new();
        private bool   _ollamaDetected;
        private string _ollamaDetectStatus = "Not checked";

        public string OllamaBaseUrl
        {
            get => _ollamaBaseUrl;
            set { _ollamaBaseUrl = value; OnPropertyChanged(); }
        }
        public string OllamaModel
        {
            get => _ollamaModel;
            set { _ollamaModel = value; OnPropertyChanged(); }
        }
        public ObservableCollection<string> OllamaModels
        {
            get => _ollamaModels;
            private set { _ollamaModels = value; OnPropertyChanged(); }
        }
        public bool OllamaDetected
        {
            get => _ollamaDetected;
            private set
            {
                _ollamaDetected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OllamaStatusColor));
            }
        }
        public string OllamaStatusColor => _ollamaDetected ? "#4CAF50" : "#9E9E9E";
        public string OllamaDetectStatus
        {
            get => _ollamaDetectStatus;
            private set { _ollamaDetectStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Probe Ollama at the configured URL, populate OllamaModels,
        /// and auto-select the first model if none is chosen.
        /// Called on startup and when the user clicks "Detect".
        /// </summary>
        public async Task DetectOllamaAsync()
        {
            OllamaDetectStatus = "Detecting...";
            OllamaDetected     = false;

            var svc  = new AiService(BuildAiSettings());
            var probe = await svc.ProbeOllamaAsync(_ollamaBaseUrl);

            if (probe.IsAvailable)
            {
                OllamaDetected     = true;
                OllamaDetectStatus = $"✓ Ollama running — {probe.Models.Count} model(s) found";
                OllamaModels.Clear();
                foreach (var m in probe.Models) OllamaModels.Add(m);

                // Auto-select if nothing chosen yet
                if (string.IsNullOrWhiteSpace(OllamaModel) && probe.Models.Count > 0)
                    OllamaModel = probe.Models[0];
            }
            else
            {
                OllamaDetected     = false;
                OllamaDetectStatus = $"✗ {probe.Error}";
            }
        }

        /// <summary>Send a minimal test prompt to the active provider.</summary>
        public async Task TestAiProviderAsync()
        {
            if (!CanTestAi) return;
            AiIsTestingProvider = true;
            AiStatus            = "Testing...";
            AiStatusColor       = "Gray";

            var svc    = new AiService(BuildAiSettings());
            var (ok, msg) = await svc.TestProviderAsync();

            AiStatus      = msg;
            AiStatusColor = ok ? "Green" : "Red";
            AiIsTestingProvider = false;
        }

        // ── Generation settings ───────────────────────────────────────────────
        private double _aiTemperature = 0.3;
        private int    _aiMaxTokens   = 1024;

        public double AiTemperature
        {
            get => _aiTemperature;
            set { _aiTemperature = value; OnPropertyChanged(); }
        }
        public int AiMaxTokens
        {
            get => _aiMaxTokens;
            set { _aiMaxTokens = value; OnPropertyChanged(); }
        }

        /// <summary>Build a transient AiSettings from current UI values (used for test + probe).</summary>
        public AiSettings BuildAiSettings() => new()
        {
            Provider      = Enum.TryParse<Core.AI.AiProvider>(_aiProvider, out var p) ? p : Core.AI.AiProvider.None,
            ClaudeApiKey  = _claudeApiKey,
            ClaudeModel   = _claudeModel,
            OpenAiApiKey  = _openAiApiKey,
            OpenAiModel   = _openAiModel,
            OllamaBaseUrl = _ollamaBaseUrl,
            OllamaModel   = _ollamaModel,
            Temperature   = _aiTemperature,
            MaxTokens     = _aiMaxTokens
        };

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // SAVE / LOAD / CANCEL
        // ════════════════════════════════════════════════════════════════════════
        #region Persistence

        private string _authToken = string.Empty;

        /// <summary>Readable by SettingsWindow to guard catalog loading.</summary>
        public string AuthToken => _authToken;

        /// <summary>
        /// Called from SettingsWindow after it opens — loads catalogs that require auth.
        /// </summary>
        public void SetAuthToken(string token)
        {
            _authToken = token ?? string.Empty;
        }

        private void LoadCurrentSettings()
        {
            // Connection
            ApiBaseUrl            = _appSettings.ApiBaseUrl ?? "http://localhost:5258";
            DefaultUser           = !string.IsNullOrEmpty(_appSettings.DefaultUser) ? _appSettings.DefaultUser : UserSettingsManager.GetDefaultUsername();
            DefaultPassword       = !string.IsNullOrEmpty(_appSettings.DefaultPassword) ? _appSettings.DefaultPassword : UserSettingsManager.GetDefaultPassword();
            DefaultUserName       = _appSettings.DefaultUserName ?? "Developer";
            EnableDetailedErrors  = _appSettings.EnableDetailedErrors;
            HttpTimeoutSeconds    = (int)_appSettings.HttpTimeout.TotalSeconds;

            // Doctor
            DoctorName      = _appSettings.DoctorName;
            DoctorTitle     = _appSettings.DoctorTitle;
            DoctorSpecialty = _appSettings.DoctorSpecialty;
            DoctorLicense   = _appSettings.DoctorLicense;
            ClinicName      = _appSettings.ClinicName;
            ClinicPhone     = _appSettings.ClinicPhone;

            // Appearance
            ColorScheme      = _appSettings.ColorScheme;
            IsDarkMode       = _appSettings.IsDarkMode;
            PreviewAccentHex = ThemeService.ResolveAccent(ColorScheme, DoctorSpecialty);

            // AI
            AiProvider    = _appSettings.AiProvider;
            ClaudeApiKey  = _appSettings.ClaudeApiKey;
            ClaudeModel   = _appSettings.ClaudeModel;
            OpenAiApiKey  = _appSettings.OpenAiApiKey;
            OpenAiModel   = _appSettings.OpenAiModel;
            OllamaBaseUrl = _appSettings.OllamaBaseUrl;
            OllamaModel   = _appSettings.OllamaModel;
        }

        private void BuildStaticLists() { /* lists are static — nothing to do */ }

        public void SaveSettings()
        {
            try
            {
                // Write directly through the IAppSettingsService interface — no fragile type-cast needed
                _appSettings.ApiBaseUrl           = ApiBaseUrl;
                _appSettings.DefaultUser          = DefaultUser;
                _appSettings.DefaultPassword      = DefaultPassword;
                _appSettings.DefaultUserName      = DefaultUserName;
                _appSettings.EnableDetailedErrors = EnableDetailedErrors;
                _appSettings.HttpTimeout          = TimeSpan.FromSeconds(HttpTimeoutSeconds);

                _appSettings.DoctorName      = DoctorName;
                _appSettings.DoctorTitle     = DoctorTitle;
                _appSettings.DoctorSpecialty = DoctorSpecialty;
                _appSettings.DoctorLicense   = DoctorLicense;
                _appSettings.ClinicName      = ClinicName;
                _appSettings.ClinicPhone     = ClinicPhone;

                _appSettings.ColorScheme = ColorScheme;
                _appSettings.IsDarkMode  = IsDarkMode;

                // AI
                _appSettings.AiProvider    = AiProvider;
                _appSettings.ClaudeApiKey  = ClaudeApiKey;
                _appSettings.ClaudeModel   = ClaudeModel;
                _appSettings.OpenAiApiKey  = OpenAiApiKey;
                _appSettings.OpenAiModel   = OpenAiModel;
                _appSettings.OllamaBaseUrl = OllamaBaseUrl;
                _appSettings.OllamaModel   = OllamaModel;

                // Hot-swap the live singleton so changes take effect immediately
                _aiService.ApplySettings(BuildAiSettings());
                _logger.LogInformation("AI settings hot-swapped to provider: {Provider}", AiProvider);

                UserSettingsManager.SaveCredentials(DefaultUser ?? string.Empty, DefaultPassword ?? string.Empty);

                // Persist to disk so settings survive restart
                _appSettings.Persist();

                // Apply theme immediately
                ThemeService.Apply(_appSettings);

                _logger.LogInformation("Settings saved and theme applied");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving settings");
                throw;
            }
        }

        public void CancelSettings() => LoadCurrentSettings();

        #endregion

        // ════════════════════════════════════════════════════════════════════════
        // API HELPERS (direct HTTP for catalog CRUD — not in IUserService)
        // ════════════════════════════════════════════════════════════════════════
        #region Direct API helpers

        private System.Net.Http.HttpRequestMessage BuildRequest(
            System.Net.Http.HttpMethod method, string url, object? body = null)
        {
            var req = new System.Net.Http.HttpRequestMessage(method, url);
            req.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authToken);
            if (body is not null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(body);
                req.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            return req;
        }

        private async Task PostTestCatalogAsync(TestCatalogCreateDto dto)
        {
            var req  = BuildRequest(System.Net.Http.HttpMethod.Post, $"{ApiBaseUrl}/api/TestCatalogs", dto);
            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        private async Task DeleteTestCatalogAsync(int id)
        {
            var req  = BuildRequest(System.Net.Http.HttpMethod.Delete, $"{ApiBaseUrl}/api/TestCatalogs/{id}");
            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        private async Task PostDrugCatalogAsync(DrugCreateDto dto)
        {
            var req  = BuildRequest(System.Net.Http.HttpMethod.Post, $"{ApiBaseUrl}/api/DrugCatalogs", dto);
            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        private async Task DeleteDrugCatalogAsync(int id)
        {
            var req  = BuildRequest(System.Net.Http.HttpMethod.Delete, $"{ApiBaseUrl}/api/DrugCatalogs/{id}");
            var resp = await _httpClient.SendAsync(req);
            resp.EnsureSuccessStatusCode();
        }

        #endregion
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
