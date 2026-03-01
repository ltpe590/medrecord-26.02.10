using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _svc;
        public AppointmentsController(IAppointmentService svc) => _svc = svc;

        // GET: api/appointments/day?date=2026-02-28
        [HttpGet("day")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetDay([FromQuery] DateTime date)
            => await _svc.GetForDayAsync(date);

        // GET: api/appointments/week?weekStart=2026-02-24
        [HttpGet("week")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetWeek([FromQuery] DateTime weekStart)
            => await _svc.GetForWeekAsync(weekStart);

        // GET: api/appointments/patient/5
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<AppointmentDto>>> GetByPatient(int patientId)
            => await _svc.GetForPatientAsync(patientId);

        // POST: api/appointments
        [HttpPost]
        public async Task<ActionResult<AppointmentDto>> Create(AppointmentCreateDto dto)
        {
            try
            {
                var result = await _svc.CreateAsync(dto);
                return CreatedAtAction(nameof(GetByPatient),
                    new { patientId = result.PatientId }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException       ex) { return BadRequest(ex.Message); }
        }

        // PATCH: api/appointments/5/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] AppointmentStatus status)
        {
            try   { await _svc.UpdateStatusAsync(id, status); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException    ex) { return BadRequest(ex.Message); }
        }

        // PATCH: api/appointments/5/reschedule
        [HttpPatch("{id}/reschedule")]
        public async Task<IActionResult> Reschedule(int id, [FromBody] DateTime newTime)
        {
            try   { await _svc.RescheduleAsync(id, newTime); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (ArgumentException    ex) { return BadRequest(ex.Message); }
        }

        // DELETE: api/appointments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Cancel(int id)
        {
            try   { await _svc.CancelAsync(id); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }
    }
}
