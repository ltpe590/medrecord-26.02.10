using Core.Data.Context;
using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VisitsController> _logger;
        private readonly IVisitService _visitService;

        public VisitsController(ApplicationDbContext context, ILogger<VisitsController> logger, IVisitService visitService)
        {
            _visitService = visitService;
            _context = context;
            _logger = logger;
        }

        // GET: api/Visits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Visit>>> GetVisits()
        {
            // Include Patient data for context in the visit list
            return await _context.Visits.ToListAsync();
        }

        // GET: api/Visits/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Visit>> GetVisit(int id)
        {
            var visit = await _context.Visits
                .Include(v => v.Entries)
                .FirstOrDefaultAsync(v => v.VisitId == id);

            if (visit == null)
            {
                return NotFound();
            }

            return visit;
        }

        // GET: api/Visits/patient/5
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Visit>>> GetVisitsByPatientId(int patientId)
        {
            var visits = await _context.Visits
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.StartedAt)
                .ToListAsync();

            return Ok(visits);
        }

        // PUT: api/Visits/5
        // Updates an existing visit record.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVisit(int id, Visit visit)
        {
            _logger.LogWarning("Direct visit updates are disabled. VisitId={VisitId}", id);
            return StatusCode(StatusCodes.Status405MethodNotAllowed,
                "Direct visit updates are disabled. Use workflow actions (start/resume/pause/end) via IVisitService.");
        }

        // POST: api/Visits
        // Creates a new visit record. Requires a valid PatientId in the request body.
        [HttpPost("start")]
        public async Task<ActionResult<VisitStartResultDto>> StartVisit(
        [FromBody] VisitStartRequestDto dto)
        {
            var result = await _visitService.StartOrResumeVisitAsync(
                dto.PatientId,
                dto.PresentingSymptom,
                dto.Duration,
                dto.ShortNote);

            return Ok(result);
        }


        // DELETE: api/Visits/5
        // Deletes a specific visit record.
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVisit(int id)
        {
            _logger.LogWarning("Direct visit deletion is disabled. VisitId={VisitId}", id);
            return StatusCode(StatusCodes.Status405MethodNotAllowed,
                "Direct visit deletion is disabled. End visits using the visit workflow.");
        }

        // Helper method to check if a visit exists
        private bool VisitExists(int id)
        {
            return _context.Visits.Any(e => e.VisitId == id);
        }
    }
}
