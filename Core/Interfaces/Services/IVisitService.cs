using Core.DTOs;

namespace Core.Interfaces.Services
{
    public interface IVisitService
    {
        Task<VisitStartResultDto> StartOrResumeVisitAsync(int patientId, string presentingSymptom, string? duration, string? shortNote);

        Task<List<PausedVisitDto>> GetPausedVisitsTodayAsync();

        Task<List<PausedVisitDto>> GetStalePausedVisitsAsync();

        Task<VisitSaveResult> SaveVisitAsync(VisitSaveRequest request);

        Task SaveObGyneGpaAsync(int visitId, DTOs.ObGyne.GPADto gpa);

        Task PauseVisitAsync(int visitId);

        Task ResumeVisitAsync(int visitId);

        Task EndVisitAsync(int visitId);

        Task<List<VisitDto>> GetVisitHistoryForPatientAsync(int patientId);
    }
}