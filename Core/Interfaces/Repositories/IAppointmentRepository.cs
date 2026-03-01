using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Repositories
{
    public interface IAppointmentRepository
    {
        Task<List<AppointmentDto>> GetByDateRangeAsync(DateTime from, DateTime to);
        Task<List<AppointmentDto>> GetByPatientAsync(int patientId);
        Task<Appointment?> GetByIdAsync(int appointmentId);
        void Add(Appointment appointment);
        void Remove(Appointment appointment);
        Task SaveChangesAsync();
    }
}
