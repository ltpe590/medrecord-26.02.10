using Core.DTOs;
using Core.Interfaces;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Core.Http
{
    public sealed class PatientHttpClient : IPatientHttpClient
    {
        private readonly IApiService _apiService;
        private readonly ILogger<PatientHttpClient> _logger;

        public PatientHttpClient(IApiService apiService, ILogger<PatientHttpClient> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Get single patient by ID
        public async Task<PatientDto?> GetPatientByIdAsync(int id, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Getting patient by ID: {PatientId}", id);
                return await _apiService.GetAsync<PatientDto>($"/api/patients/{id}", ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient by ID {PatientId}", id);
                throw;
            }
        }

        // Get all patients (list) - Clear naming!
        public async Task<List<PatientDto>> GetAllPatientsListAsync(CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Getting all patients list");
                var patients = await _apiService.GetAsync<List<PatientDto>>("/api/patients", ct);

                _logger.LogInformation("Retrieved {PatientCount} patients from API", patients?.Count ?? 0);
                return patients ?? new List<PatientDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patients list");
                throw;
            }
        }

        // Search patients by term
        public async Task<List<PatientDto>> SearchPatientsAsync(string searchTerm, CancellationToken ct = default)
        {
            try
            {
                _logger.LogDebug("Searching patients with term: {SearchTerm}", searchTerm);

                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    // If search term is empty, return all patients
                    return await GetAllPatientsListAsync(ct);
                }

                var patients = await _apiService.GetAsync<List<PatientDto>>($"/api/patients/search?term={Uri.EscapeDataString(searchTerm)}", ct);

                _logger.LogDebug("Found {PatientCount} patients matching '{SearchTerm}'",
                    patients?.Count ?? 0, searchTerm);

                return patients ?? new List<PatientDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        // Create patient
        public async Task<PatientDto?> CreatePatientAsync(PatientCreateDto dto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Creating new patient: {PatientName}", dto.Name);

                var createdPatient = await _apiService.PostAsync<PatientCreateDto, PatientDto>("/api/patients", dto, ct);

                _logger.LogInformation("Patient created successfully: {PatientName} (ID: {PatientId})",
                    createdPatient?.Name, createdPatient?.PatientId);

                return createdPatient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {PatientName}", dto.Name);
                throw;
            }
        }

        // Update patient
        public async Task<bool> UpdatePatientAsync(PatientUpdateDto dto, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Updating patient ID: {PatientId}", dto.PatientId);

                await _apiService.PutAsync($"/api/patients/{dto.PatientId}", dto, ct);

                _logger.LogInformation("Patient {PatientId} updated successfully", dto.PatientId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", dto.PatientId);
                throw;
            }
        }

        // Delete patient
        public async Task<bool> DeletePatientAsync(int id, CancellationToken ct = default)
        {
            try
            {
                _logger.LogInformation("Deleting patient ID: {PatientId}", id);

                await _apiService.DeleteAsync($"/api/patients/{id}", ct);

                _logger.LogInformation("Patient {PatientId} deleted successfully", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                throw;
            }
        }
    }
}