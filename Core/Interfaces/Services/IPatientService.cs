using Core.DTOs;
using Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces.Services
{
    public interface IPatientService
    {
        // Get single patient by ID
        Task<PatientDto?> GetPatientByIdAsync(int id);

        // Get all patients (list) - Clear naming!
        Task<List<PatientDto>> GetAllPatientsListAsync();

        // Search patients
        Task<List<PatientDto>> SearchPatientsAsync(string searchTerm);

        // CRUD operations
        Task<PatientDto?> CreatePatientAsync(PatientCreateDto dto);
        Task<bool> UpdatePatientAsync(PatientUpdateDto dto);
        Task<bool> DeletePatientAsync(int id);

    }
}
