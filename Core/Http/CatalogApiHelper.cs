using Core.DTOs;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Core.Http
{
    /// <summary>
    /// Lightweight catalog CRUD helper used by SettingsViewModel.
    /// Uses its own HttpClient so it doesn't affect the shared ApiService state.
    /// </summary>
    public sealed class CatalogApiHelper
    {
        private readonly HttpClient _http;
        private readonly string     _baseUrl;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy        = JsonNamingPolicy.CamelCase
        };

        private CatalogApiHelper(string baseUrl, string token)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _http    = new HttpClient();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _http.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static CatalogApiHelper CreateHelper(string baseUrl, string token)
            => new(baseUrl, token);

        // ── Tests ────────────────────────────────────────────────────────────

        public async Task PostTestAsync(TestCatalogCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/TestCatalogs", dto, _json);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteTestAsync(int testId)
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/api/TestCatalogs/{testId}");
            response.EnsureSuccessStatusCode();
        }

        // ── Drugs ────────────────────────────────────────────────────────────

        public async Task PostDrugAsync(DrugCreateDto dto)
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/api/DrugCatalogs", dto, _json);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteDrugAsync(int drugId)
        {
            var response = await _http.DeleteAsync($"{_baseUrl}/api/DrugCatalogs/{drugId}");
            response.EnsureSuccessStatusCode();
        }
    }
}
