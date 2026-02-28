using Core.DTOs;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    /// <summary>
    /// Orchestrates patient data access: local DB first, HTTP fallback.
    /// All entity↔DTO mapping is delegated to IPatientMappingService.
    /// Cache-write on UpdatePatientAsync is a deliberate best-effort sync;
    /// failures are logged as warnings and do not fail the operation.
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository     _patientRepository;
        private readonly IPatientHttpClient     _patientHttpClient;
        private readonly IAppSettingsService    _appSettings;
        private readonly IPatientMappingService _mapper;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository      patientRepository,
            IPatientHttpClient      patientHttpClient,
            IAppSettingsService     appSettings,
            IPatientMappingService  mapper,
            ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _patientHttpClient = patientHttpClient ?? throw new ArgumentNullException(nameof(patientHttpClient));
            _appSettings       = appSettings       ?? throw new ArgumentNullException(nameof(appSettings));
            _mapper            = mapper            ?? throw new ArgumentNullException(nameof(mapper));
            _logger            = logger            ?? throw new ArgumentNullException(nameof(logger));
        }

        // ── Queries ───────────────────────────────────────────────────────────

        public async Task<PatientDto?> GetPatientByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting patient by ID: {PatientId}", id);

                var local = await _patientRepository.GetByIdAsync(id);
                if (local != null)
                {
                    _logger.LogDebug("Patient {PatientId} found in local repository", id);
                    return _mapper.MapToDto(local);
                }

                _logger.LogDebug("Patient {PatientId} not found locally, checking API", id);
                return await _patientHttpClient.GetPatientByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient by ID {PatientId}", id);
                throw;
            }
        }

        public async Task<List<PatientDto>> GetAllPatientsListAsync()
        {
            try
            {
                _logger.LogDebug("Getting all patients list");

                var locals = await _patientRepository.GetAllAsync();
                if (locals?.Any() == true)
                {
                    _logger.LogDebug("Found {Count} patients in local repository", locals.Count());
                    return locals.Select(_mapper.MapToDto).ToList();
                }

                _logger.LogDebug("No patients found locally, checking API");
                return await _patientHttpClient.GetAllPatientsListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all patients list");
                throw;
            }
        }

        public async Task<List<PatientDto>> SearchPatientsAsync(string searchTerm)
        {
            try
            {
                _logger.LogDebug("Searching patients with term: {SearchTerm}", searchTerm);
                return await _patientHttpClient.SearchPatientsAsync(searchTerm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        // ── Commands ──────────────────────────────────────────────────────────

        public async Task<PatientDto?> CreatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new patient: {PatientName}", dto.Name);
                // API owns the write; do NOT also call _patientRepository — same DB, would duplicate.
                return await _patientHttpClient.CreatePatientAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {PatientName}", dto.Name);
                throw;
            }
        }

        public async Task<bool> UpdatePatientAsync(PatientUpdateDto dto)
        {
            try
            {
                _logger.LogInformation("Updating patient ID: {PatientId}", dto.PatientId);

                var success = await _patientHttpClient.UpdatePatientAsync(dto);

                if (success)
                {
                    // Best-effort local cache sync — failure is non-fatal.
                    try
                    {
                        var local = await _patientRepository.GetByIdAsync(dto.PatientId);
                        if (local != null)
                        {
                            _mapper.MapUpdateDtoToDomain(dto, local);
                            await _patientRepository.UpdateAsync(local);
                            _logger.LogDebug("Local cache updated for patient {PatientId}", dto.PatientId);
                        }
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "Failed to update local cache for patient {PatientId}", dto.PatientId);
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient {PatientId}", dto.PatientId);
                throw;
            }
        }

        public async Task<bool> DeletePatientAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting patient ID: {PatientId}", id);

                var success = await _patientHttpClient.DeletePatientAsync(id);

                if (success)
                {
                    // Best-effort local cache removal — failure is non-fatal.
                    try
                    {
                        await _patientRepository.DeleteAsync(id);
                        _logger.LogDebug("Patient {PatientId} removed from local cache", id);
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "Failed to remove patient {PatientId} from local cache", id);
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting patient {PatientId}", id);
                throw;
            }
        }
    }
}
