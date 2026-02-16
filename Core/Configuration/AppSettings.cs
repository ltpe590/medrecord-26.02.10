using Core.Interfaces.Services;

namespace Core.Configuration
{
    public class AppSettings : IAppSettingsService
    {
        public string ApiBaseUrl { get; set; } = "http://localhost:5258";
        public string? DefaultUser { get; set; }
        public string? DefaultPassword { get; set; }
        public string? DefaultUserName { get; set; }
        public bool EnableDetailedErrors { get; set; } = false;
        public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public string ConnectionString { get; set; } = "Server=localhost;Database=MedRecordsDB;Trusted_Connection=true;TrustServerCertificate=true;";
        public int DaysToKeepPausedVisits { get; set; } = 7;
        public string LogDirectory { get; set; } = "Logs";
        public string PausedVisitsDirectory { get; set; } = "PausedVisits";

        // Database settings
        public int MaxRetryCount { get; set; } = 3;

        public int CommandTimeout { get; set; } = 30;

        // Security settings
        public bool EnableFingerprint { get; set; } = false;

        public int SessionTimeoutMinutes { get; set; } = 30;
    }
}