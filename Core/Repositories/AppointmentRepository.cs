using Core.Data.Context;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public sealed class AppointmentRepository : IAppointmentRepository
    {
        private readonly ApplicationDbContext _ctx;
        public AppointmentRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<List<AppointmentDto>> GetByDateRangeAsync(DateTime from, DateTime to) =>
            _ctx.Appointments
                .Include(a => a.Patient)
                .Where(a => a.ScheduledAt >= from && a.ScheduledAt < to)
                .OrderBy(a => a.ScheduledAt)
                .AsNoTracking()
                .Select(a => new AppointmentDto
                {
                    AppointmentId = a.AppointmentId,
                    PatientId     = a.PatientId,
                    PatientName   = a.Patient!.Name,
                    ScheduledAt   = a.ScheduledAt,
                    Reason        = a.Reason,
                    Notes         = a.Notes,
                    Status        = a.Status
                })
                .ToListAsync();

        public Task<List<AppointmentDto>> GetByPatientAsync(int patientId) =>
            _ctx.Appointments
                .Include(a => a.Patient)
                .Where(a => a.PatientId == patientId)
                .OrderByDescending(a => a.ScheduledAt)
                .AsNoTracking()
                .Select(a => new AppointmentDto
                {
                    AppointmentId = a.AppointmentId,
                    PatientId     = a.PatientId,
                    PatientName   = a.Patient!.Name,
                    ScheduledAt   = a.ScheduledAt,
                    Reason        = a.Reason,
                    Notes         = a.Notes,
                    Status        = a.Status
                })
                .ToListAsync();

        public Task<Appointment?> GetByIdAsync(int id) =>
            _ctx.Appointments.FindAsync(id).AsTask();

        public void Add(Appointment appointment) =>
            _ctx.Appointments.Add(appointment);

        public void Remove(Appointment appointment) =>
            _ctx.Appointments.Remove(appointment);

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}
