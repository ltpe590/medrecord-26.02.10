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
        

        // PUT: api/Visits/5
        // Updates an existing visit record.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVisit(int id, Visit visit)
        {
            if (id != visit.VisitId)
            {
                return BadRequest();
            }

            _context.Entry(visit).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VisitExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // HTTP 204 No Content
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
            var visit = await _context.Visits.FindAsync(id);
            if (visit == null)
            {
                return NotFound();
            }

            _context.Visits.Remove(visit);
            await _context.SaveChangesAsync();

            return NoContent(); // HTTP 204 No Content
        }

        // Helper method to check if a visit exists
        private bool VisitExists(int id)
        {
            return _context.Visits.Any(e => e.VisitId == id);
        }
    }
}
