namespace WPF.Configuration
{
    /// <summary>
    /// WPF-layer strong-typed wrapper. Values loaded from appsettings.json at startup.
    /// Note: this is the WPF-side copy — the authoritative runtime object is
    /// Core.Configuration.AppSettings which implements IAppSettingsService.
    /// </summary>
    public sealed class AppSettings
    {
        public const string SectionName = "AppSettings";

        public string? ApiBaseUrl      { get; set; }
        public string? DefaultUser     { get; set; }
        public string? DefaultPassword { get; set; }
        public string? DefaultUserName { get; set; }
        public bool    EnableDetailedErrors { get; set; } = false;
        public TimeSpan HttpTimeout    { get; set; } = TimeSpan.FromSeconds(30);

        public string DoctorName      { get; set; } = string.Empty;
        public string DoctorTitle     { get; set; } = "Dr.";
        public string DoctorSpecialty { get; set; } = "General";
        public string DoctorLicense   { get; set; } = string.Empty;
        public string ClinicName      { get; set; } = string.Empty;

        public string ColorScheme { get; set; } = "SpecialtyLinked";
        public bool   IsDarkMode  { get; set; } = false;
    }
}
