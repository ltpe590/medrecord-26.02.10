namespace Core.Interfaces.Services
{
    public interface IAppSettingsService
    {
        string ApiBaseUrl { get; set; } // Must have { get; set; }
        string? DefaultUser { get; set; }
        string? DefaultPassword { get; set; }
        string? DefaultUserName { get; set; }
        string ConnectionString { get; set; }
        TimeSpan HttpTimeout { get; set; }
        int DaysToKeepPausedVisits { get; set; }
        string LogDirectory { get; set; }
        string PausedVisitsDirectory { get; set; }
        bool EnableDetailedErrors { get; set; }
        int MaxRetryCount { get; set; }
        int CommandTimeout { get; set; }
        bool EnableFingerprint { get; set; }
        int SessionTimeoutMinutes { get; set; }
    }
}