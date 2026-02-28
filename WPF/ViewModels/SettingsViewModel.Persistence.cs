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
    public partial class SettingsViewModel
    {
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
        #endregion Direct API helpers
    }
}
