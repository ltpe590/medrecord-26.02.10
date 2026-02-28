using Core.Interfaces.Services;

namespace Core.Configuration
{
    public class AppSettings : IAppSettingsService
    {
        // ── Connection ────────────────────────────────────────────────────────
        public string ApiBaseUrl { get; set; } = "http://localhost:5258";
        public string? DefaultUser { get; set; }
        public string? DefaultPassword { get; set; }
        public string? DefaultUserName { get; set; }
        public bool EnableDetailedErrors { get; set; } = false;
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public string ConnectionString { get; set; } = "Server=localhost;Database=MedRecordsDB;Trusted_Connection=true;TrustServerCertificate=true;";
        public int MaxRetryCount { get; set; } = 3;
        public int CommandTimeout { get; set; } = 30;

        // ── Application ───────────────────────────────────────────────────────
        public int DaysToKeepPausedVisits { get; set; } = 7;
        public string LogDirectory { get; set; } = "Logs";
        public string PausedVisitsDirectory { get; set; } = "PausedVisits";
        public bool EnableFingerprint { get; set; } = false;
        public int SessionTimeoutMinutes { get; set; } = 30;

        /// <summary>BCP-47 culture tag, e.g. "en-US", "ar-SA", "he-IL", "fa-IR".</summary>
        public string Language { get; set; } = "en-US";

        // ── Doctor profile ────────────────────────────────────────────────────
        public string DoctorName { get; set; } = string.Empty;
        public string DoctorTitle { get; set; } = "Dr.";
        public string DoctorSpecialty { get; set; } = "General";
        public string DoctorLicense { get; set; } = string.Empty;
        public string ClinicName { get; set; } = string.Empty;
        public string ClinicPhone { get; set; } = string.Empty;

        // ── Clinic locale ─────────────────────────────────────────────────────
        /// <summary>IANA / Windows timezone ID for the clinic.</summary>
        public string ClinicTimeZoneId { get; set; } = "Asia/Baghdad";

        // ── Appearance ────────────────────────────────────────────────────────
        public string ColorScheme { get; set; } = "SpecialtyLinked";
        public bool IsDarkMode { get; set; } = false;

        // ── AI provider ───────────────────────────────────────────────────────
        public string AiProvider    { get; set; } = "None";
        public string ClaudeApiKey  { get; set; } = string.Empty;
        public string ClaudeModel   { get; set; } = "claude-opus-4-6";
        public string OpenAiApiKey  { get; set; } = string.Empty;
        public string OpenAiModel   { get; set; } = "gpt-4o";
        public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
        public string OllamaModel   { get; set; } = string.Empty;

        /// <summary>
        /// No-op for the Core model object — persistence is handled by AppSettingsService.
        /// </summary>
        public void Persist() { }
    }
}
