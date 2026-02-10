using Core.Configuration;
using Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Core.Services
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly AppSettings _settings;

        public AppSettingsService(IConfiguration configuration)
        {
            _settings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(_settings);
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
    }
}
