namespace Core.Interfaces.Services
{
    public interface IAppSettingsService
    {
        // ── Connection ────────────────────────────────────────────────────────
        string ApiBaseUrl { get; set; }
        string? DefaultUser { get; set; }
        string? DefaultPassword { get; set; }
        string? DefaultUserName { get; set; }
        string ConnectionString { get; set; }
        TimeSpan HttpTimeout { get; set; }
        bool EnableDetailedErrors { get; set; }
        int MaxRetryCount { get; set; }
        int CommandTimeout { get; set; }

        // ── Application ───────────────────────────────────────────────────────
        int DaysToKeepPausedVisits { get; set; }
        string LogDirectory { get; set; }
        string PausedVisitsDirectory { get; set; }
        bool EnableFingerprint { get; set; }
        int SessionTimeoutMinutes { get; set; }
        string Language { get; set; }

        // ── Doctor profile ────────────────────────────────────────────────────
        string DoctorName { get; set; }
        string DoctorTitle { get; set; }       // "Dr." / "Prof." / "Mr." / "Ms."
        string DoctorSpecialty { get; set; }   // matches ClinicalSystem.Name
        string DoctorLicense { get; set; }
        string ClinicName { get; set; }
        string ClinicPhone { get; set; }

        // ── Appearance ────────────────────────────────────────────────────────
        string ColorScheme { get; set; }       // "SpecialtyLinked" | "Blue" | …
        bool IsDarkMode { get; set; }

        // ── AI provider ───────────────────────────────────────────────────────
        string AiProvider    { get; set; }     // "None" | "Claude" | "ChatGpt" | "Ollama"
        string ClaudeApiKey  { get; set; }
        string ClaudeModel   { get; set; }
        string OpenAiApiKey  { get; set; }
        string OpenAiModel   { get; set; }
        string OllamaBaseUrl { get; set; }
        string OllamaModel   { get; set; }

        /// <summary>Persist in-memory settings back to appsettings.json.</summary>
        void Persist();
    }
}
