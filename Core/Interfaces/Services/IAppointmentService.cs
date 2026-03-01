using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IAppointmentService
    {
        Task<List<AppointmentDto>> GetForDayAsync(DateTime date);
        Task<List<AppointmentDto>> GetForWeekAsync(DateTime weekStart);
        Task<List<AppointmentDto>> GetForPatientAsync(int patientId);
        Task<AppointmentDto>       CreateAsync(AppointmentCreateDto dto);
        Task                       RescheduleAsync(int appointmentId, DateTime newTime);
        Task                       UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus);
        Task                       CancelAsync(int appointmentId);
    }
}
