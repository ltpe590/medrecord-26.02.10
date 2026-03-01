using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;

namespace Core.Services
{
    public sealed class AppointmentService : IAppointmentService
    {
        private readonly IAppointmentRepository _repo;
        private readonly IPatientRepository     _patients;

        public AppointmentService(IAppointmentRepository repo, IPatientRepository patients)
        {
            _repo     = repo;
            _patients = patients;
        }

        public Task<List<AppointmentDto>> GetForDayAsync(DateTime date)
        {
            var from = date.Date.ToUniversalTime();
            var to   = from.AddDays(1);
            return _repo.GetByDateRangeAsync(from, to);
        }

        public Task<List<AppointmentDto>> GetForWeekAsync(DateTime weekStart)
        {
            var from = weekStart.Date.ToUniversalTime();
            var to   = from.AddDays(7);
            return _repo.GetByDateRangeAsync(from, to);
        }

        public Task<List<AppointmentDto>> GetForPatientAsync(int patientId) =>
            _repo.GetByPatientAsync(patientId);

        public async Task<AppointmentDto> CreateAsync(AppointmentCreateDto dto)
        {
            if (!await _patients.ExistsAsync(dto.PatientId))
                throw new KeyNotFoundException($"Patient {dto.PatientId} not found.");

            var appt = new Appointment(dto.PatientId, dto.ScheduledAt, dto.Reason, dto.Notes);
            _repo.Add(appt);
            await _repo.SaveChangesAsync();

            // Return DTO; re-query to get joined patient name
            var saved = (await _repo.GetByPatientAsync(dto.PatientId))
                        .First(a => a.AppointmentId == appt.AppointmentId);
            return saved;
        }

        public async Task RescheduleAsync(int appointmentId, DateTime newTime)
        {
            var appt = await GetOrThrowAsync(appointmentId);
            appt.Reschedule(newTime);
            await _repo.SaveChangesAsync();
        }

        public async Task UpdateStatusAsync(int appointmentId, AppointmentStatus newStatus)
        {
            var appt = await GetOrThrowAsync(appointmentId);
            switch (newStatus)
            {
                case AppointmentStatus.Arrived:   appt.MarkArrived(); break;
                case AppointmentStatus.Completed: appt.Complete();    break;
                case AppointmentStatus.Cancelled: appt.Cancel();      break;
                default: throw new ArgumentException($"Cannot set status to {newStatus} via this method.");
            }
            await _repo.SaveChangesAsync();
        }

        public async Task CancelAsync(int appointmentId)
        {
            var appt = await GetOrThrowAsync(appointmentId);
            appt.Cancel();
            await _repo.SaveChangesAsync();
        }

        private async Task<Appointment> GetOrThrowAsync(int id)
        {
            var appt = await _repo.GetByIdAsync(id);
            if (appt == null) throw new KeyNotFoundException($"Appointment {id} not found.");
            return appt;
        }
    }
}
