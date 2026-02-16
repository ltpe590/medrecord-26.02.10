using Core.DTOs;
using Core.Entities;

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

        Task EndVisitAsync(int visitId);

        Task<Visit?> GetActiveVisitForPatientAsync(int patientId);

        Task<List<Visit>> GetVisitHistoryForPatientAsync(int patientId);
    }
}