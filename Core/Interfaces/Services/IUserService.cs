using Core.DTOs;

public interface IUserService
{
    Task<string> LoginAsync(string username, string password, string baseUrl);

    // Collections return non-nullable lists (empty if no data)
    Task<List<PatientDto>> GetAllPatientsAsync(string baseUrl, string token);

    Task<List<VisitDto>> GetVisitsByPatientAsync(int patientId, string baseUrl, string token);

    Task<List<TestCatalogDto>> GetTestCatalogAsync(string baseUrl, string token);

    Task<List<DrugCatalogDto>> GetDrugCatalogAsync(string baseUrl, string token);

    Task<List<PrescriptionDto>> GetPrescriptionsByVisitAsync(int visitId, string baseUrl, string token);

    // Single objects can be nullable
    Task<PatientDto?> GetPatientByIdAsync(int patientId, string authToken, string apiBaseUrl);

    // Void operations (no return)
    Task CreatePatientAsync(PatientCreateDto patient, string baseUrl, string token);

    Task UpdatePatientAsync(PatientUpdateDto dto, string baseUrl, string token);

    Task DeletePatientAsync(PatientDeleteDto dto, string baseUrl, string token);

    Task SaveVisitAsync(VisitDto visit, string baseUrl, string token);

    Task SaveLabResultAsync(LabResultCreateDto dto, string baseUrl, string token);

    Task SavePrescriptionAsync(PrescriptionCreateDto dto, string baseUrl, string token);

    Task DeletePrescriptionsByVisitAsync(int visitId, string baseUrl, string token);
}