using Core.Configuration;
using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Core.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly AppSettings _settings;
        private readonly string _settingsFilePath;

        public AppSettingsService(IConfiguration configuration)
        {
            _settings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(_settings);

            // Resolve the physical appsettings.json path so we can write back to it
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var baseDir = AppContext.BaseDirectory;
            _settingsFilePath = Path.Combine(baseDir, "appsettings.json");
        }

        /// <summary>
        /// Serializes current settings back to appsettings.json so they survive restart.
        /// </summary>
        public void Persist()
        {
            try
            {
                string json = "{}";
                if (File.Exists(_settingsFilePath))
                    json = File.ReadAllText(_settingsFilePath);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement.Clone();

                // Rebuild the full document with the updated AppSettings section
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json)
                           ?? new Dictionary<string, JsonElement>();

                var settingsJson = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                dict["AppSettings"] = JsonDocument.Parse(settingsJson).RootElement.Clone();

                var output = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsFilePath, output);
            }
            catch (Exception ex)
            {
                // Non-fatal — settings are still in memory for this session
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Failed to persist settings: {ex.Message}");
            }
        }

        // CHANGE ALL PROPERTIES TO HAVE GETTERS AND SETTERS
        public string ApiBaseUrl
        {
            get => _settings.ApiBaseUrl;
            set => _settings.ApiBaseUrl = value;
        }

        public string? DefaultUser
        {
            get => _settings.DefaultUser;
            set => _settings.DefaultUser = value;
        }

        public string? DefaultPassword
        {
            get => _settings.DefaultPassword;
            set => _settings.DefaultPassword = value;
        }

        public string? DefaultUserName
        {
            get => _settings.DefaultUserName;
            set => _settings.DefaultUserName = value;
        }

        public string ConnectionString
        {
            get => _settings.ConnectionString;
            set => _settings.ConnectionString = value;
        }

        public TimeSpan HttpTimeout
        {
            get => _settings.HttpTimeout;
            set => _settings.HttpTimeout = value;
        }

        public int DaysToKeepPausedVisits
        {
            get => _settings.DaysToKeepPausedVisits;
            set => _settings.DaysToKeepPausedVisits = value;
        }

        public string LogDirectory
        {
            get => _settings.LogDirectory;
            set => _settings.LogDirectory = value;
        }

        public string PausedVisitsDirectory
        {
            get => _settings.PausedVisitsDirectory;
            set => _settings.PausedVisitsDirectory = value;
        }

        public bool EnableDetailedErrors
        {
            get => _settings.EnableDetailedErrors;
            set => _settings.EnableDetailedErrors = value;
        }

        public int MaxRetryCount
        {
            get => _settings.MaxRetryCount;
            set => _settings.MaxRetryCount = value;
        }

        public int CommandTimeout
        {
            get => _settings.CommandTimeout;
            set => _settings.CommandTimeout = value;
        }

        public bool EnableFingerprint
        {
            get => _settings.EnableFingerprint;
            set => _settings.EnableFingerprint = value;
        }

        public int SessionTimeoutMinutes
        {
            get => _settings.SessionTimeoutMinutes;
            set => _settings.SessionTimeoutMinutes = value;
        }

        public string Language
        {
            get => _settings.Language;
            set => _settings.Language = value;
        }

        // ── Doctor profile ────────────────────────────────────────────────────
        public string DoctorName
        {
            get => _settings.DoctorName;
            set => _settings.DoctorName = value;
        }

        public string DoctorTitle
        {
            get => _settings.DoctorTitle;
            set => _settings.DoctorTitle = value;
        }

        public string DoctorSpecialty
        {
            get => _settings.DoctorSpecialty;
            set => _settings.DoctorSpecialty = value;
        }

        public string DoctorLicense
        {
            get => _settings.DoctorLicense;
            set => _settings.DoctorLicense = value;
        }

        public string ClinicName
        {
            get => _settings.ClinicName;
            set => _settings.ClinicName = value;
        }

        public string ClinicPhone
        {
            get => _settings.ClinicPhone;
            set => _settings.ClinicPhone = value;
        }

        // ── Appearance ────────────────────────────────────────────────────────
        public string ColorScheme
        {
            get => _settings.ColorScheme;
            set => _settings.ColorScheme = value;
        }

        public bool IsDarkMode
        {
            get => _settings.IsDarkMode;
            set => _settings.IsDarkMode = value;
        }

        // ── AI provider ───────────────────────────────────────────────────────
        public string AiProvider
        {
            get => _settings.AiProvider;
            set => _settings.AiProvider = value;
        }
        public string ClaudeApiKey
        {
            get => _settings.ClaudeApiKey;
            set => _settings.ClaudeApiKey = value;
        }
        public string ClaudeModel
        {
            get => _settings.ClaudeModel;
            set => _settings.ClaudeModel = value;
        }
        public string OpenAiApiKey
        {
            get => _settings.OpenAiApiKey;
            set => _settings.OpenAiApiKey = value;
        }
        public string OpenAiModel
        {
            get => _settings.OpenAiModel;
            set => _settings.OpenAiModel = value;
        }
        public string OllamaBaseUrl
        {
            get => _settings.OllamaBaseUrl;
            set => _settings.OllamaBaseUrl = value;
        }
        public string OllamaModel
        {
            get => _settings.OllamaModel;
            set => _settings.OllamaModel = value;
        }
        public string ClinicTimeZoneId
        {
            get => _settings.ClinicTimeZoneId;
            set => _settings.ClinicTimeZoneId = value;
        }
    }
}