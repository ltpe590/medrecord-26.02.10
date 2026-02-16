using Core.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IPatientHttpClient _patientHttpClient;
        private readonly IAppSettingsService _appSettings;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository patientRepository,
            IPatientHttpClient patientHttpClient,
            IAppSettingsService appSettings,
            ILogger<PatientService> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _patientHttpClient = patientHttpClient ?? throw new ArgumentNullException(nameof(patientHttpClient));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<PatientDto?> GetPatientByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Getting patient by ID: {PatientId}", id);

                // Try local repository first
                var localPatient = await _patientRepository.GetByIdAsync(id);
                if (localPatient != null)
                {
                    _logger.LogDebug("Patient {PatientId} found in local repository", id);
                    return MapToDto(localPatient);
                }

                // Fall back to API
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

                // Try local repository first
                var localPatients = await _patientRepository.GetAllAsync();
                if (localPatients != null && localPatients.Any())
                {
                    _logger.LogDebug("Found {Count} patients in local repository", localPatients.Count());
                    var patientList = localPatients.ToList();
                    return MapToDtos(patientList);
                }

                // Fall back to API
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

        public async Task<PatientDto?> CreatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                _logger.LogInformation("Creating new patient: {PatientName}", dto.Name);

                // Call API to create patient
                var createdPatient = await _patientHttpClient.CreatePatientAsync(dto);

                if (createdPatient != null)
                {
                    // Cache in local repository
                    try
                    {
                        var patientEntity = MapDtoToEntity(createdPatient);
                        await _patientRepository.AddAsync(patientEntity);
                        _logger.LogDebug("Patient cached locally: {PatientId}", createdPatient.PatientId);
                    }
                    catch (Exception cacheEx)
                    {
                        _logger.LogWarning(cacheEx, "Failed to cache patient {PatientId} locally", createdPatient.PatientId);
                        // Don't fail the operation if caching fails
                    }
                }

                return createdPatient;
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

                // Update via API
                var success = await _patientHttpClient.UpdatePatientAsync(dto);

                if (success)
                {
                    // Update local cache if exists
                    try
                    {
                        var localPatient = await _patientRepository.GetByIdAsync(dto.PatientId);
                        if (localPatient != null)
                        {
                            UpdateEntityFromDto(dto, localPatient);
                            await _patientRepository.UpdateAsync(localPatient);
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

                // Delete via API
                var success = await _patientHttpClient.DeletePatientAsync(id);

                if (success)
                {
                    // Remove from local cache
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

        #region Helper Methods

        private List<PatientDto> MapToDtos(List<Patient> patients)
        {
            return patients.Select(MapToDto).ToList();
        }

        private PatientDto MapToDto(Patient patient)
        {
            return new PatientDto
            {
                PatientId = patient.PatientId,
                Name = patient.Name,
                Sex = patient.Sex,  // Direct assignment - enum to enum
                DateOfBirth = patient.DateOfBirth.ToDateTime(System.TimeOnly.MinValue),
                PhoneNumber = patient.PhoneNumber?.Value,
                BloodGroup = patient.BloodGroup,
                Allergies = patient.Allergies,
                Address = patient.Address,
                ShortNote = patient.ShortNote
            };
        }

        private Patient MapDtoToEntity(PatientDto dto)
        {
            var patient = new Patient(
                dto.Name,
                dto.Sex,  // Direct assignment - enum to enum
                DateOnly.FromDateTime(dto.DateOfBirth));

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) ||
                !string.IsNullOrWhiteSpace(dto.Address))
            {
                patient.UpdateContact(
                    string.IsNullOrWhiteSpace(dto.PhoneNumber)
                        ? null
                        : new PhoneNumber(dto.PhoneNumber),
                    dto.Address);
            }

            patient.UpdateClinicalInfo(
                dto.BloodGroup,
                dto.Allergies,
                dto.ShortNote);

            return patient;
        }

        private void UpdateEntityFromDto(PatientUpdateDto dto, Patient patient)
        {
            if (dto.Name != null || dto.Sex != null || dto.DateOfBirth.HasValue)
            {
                var sex = dto.Sex ?? patient.Sex;  // Direct nullable enum usage

                patient.UpdateIdentity(
                    dto.Name ?? patient.Name,
                    sex,
                    dto.DateOfBirth ?? patient.DateOfBirth);
            }

            if (dto.PhoneNumber != null || dto.Address != null)
            {
                patient.UpdateContact(
                    dto.PhoneNumber != null ? new PhoneNumber(dto.PhoneNumber) : patient.PhoneNumber,
                    dto.Address ?? patient.Address);
            }

            if (dto.BloodGroup != null || dto.Allergies != null || dto.ShortNote != null)
            {
                patient.UpdateClinicalInfo(
                    dto.BloodGroup ?? patient.BloodGroup,
                    dto.Allergies ?? patient.Allergies,
                    dto.ShortNote ?? patient.ShortNote);
            }
        }

        #endregion Helper Methods
    }
}