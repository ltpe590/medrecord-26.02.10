using Core.DTOs;

namespace Core.Interfaces
{
    public interface IPatientHttpClient
    {
        // Get single patient by ID
        Task<PatientDto?> GetPatientByIdAsync(int id, CancellationToken ct = default);

        // Get all patients (list) - Clear naming!
        Task<List<PatientDto>> GetAllPatientsListAsync(CancellationToken ct = default);

        // Search/filter methods
        Task<List<PatientDto>> SearchPatientsAsync(string searchTerm, CancellationToken ct = default);

        // CRUD operations
        Task<PatientDto?> CreatePatientAsync(PatientCreateDto dto, CancellationToken ct = default);
        Task<bool> UpdatePatientAsync(PatientUpdateDto dto, CancellationToken ct = default);
        Task<bool> DeletePatientAsync(int id, CancellationToken ct = default);
    }
}
