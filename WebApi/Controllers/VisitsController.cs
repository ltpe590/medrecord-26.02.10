using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VisitsController : ControllerBase
    {
        private readonly IVisitRepository         _repo;
        private readonly IVisitService            _visitService;
        private readonly ILogger<VisitsController> _logger;

        public VisitsController(
            IVisitRepository          repo,
            IVisitService             visitService,
            ILogger<VisitsController> logger)
        {
            _repo         = repo;
            _visitService = visitService;
            _logger       = logger;
        }

        // GET: api/Visits
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Visit>>> GetVisits() =>
            await _repo.GetAllAsync();

        // GET: api/Visits/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Visit>> GetVisit(int id)
        {
            var visit = await _repo.GetWithDetailsAsync(id);
            return visit == null ? NotFound() : visit;
        }

        // GET: api/Visits/patient/5
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Visit>>> GetVisitsByPatientId(int patientId) =>
            await _repo.GetByPatientIdAsync(patientId);

        // PUT: api/Visits/5  — disabled by design
        [HttpPut("{id}")]
        public IActionResult PutVisit(int id)
        {
            _logger.LogWarning("Direct visit update attempted. VisitId={VisitId}", id);
            return StatusCode(StatusCodes.Status405MethodNotAllowed,
                "Direct visit updates are disabled. Use workflow actions (start/pause/resume/end).");
        }

        // POST: api/Visits/start
        [HttpPost("start")]
        public async Task<ActionResult<VisitStartResultDto>> StartVisit([FromBody] VisitStartRequestDto dto)
        {
            var result = await _visitService.StartOrResumeVisitAsync(
                dto.PatientId, dto.PresentingSymptom, dto.Duration, dto.ShortNote);
            return Ok(result);
        }

        // POST: api/Visits/{id}/pause
        [HttpPost("{id}/pause")]
        public async Task<IActionResult> PauseVisit(int id)
        {
            if (!await _repo.ExistsAsync(id)) return NotFound($"Visit {id} not found.");
            await _visitService.PauseVisitAsync(id);
            return NoContent();
        }

        // POST: api/Visits/{id}/resume
        [HttpPost("{id}/resume")]
        public async Task<IActionResult> ResumeVisit(int id)
        {
            if (!await _repo.ExistsAsync(id)) return NotFound($"Visit {id} not found.");
            await _visitService.ResumeVisitAsync(id);
            return NoContent();
        }

        // POST: api/Visits/{id}/end
        [HttpPost("{id}/end")]
        public async Task<IActionResult> EndVisit(int id)
        {
            if (!await _repo.ExistsAsync(id)) return NotFound($"Visit {id} not found.");
            await _visitService.EndVisitAsync(id);
            return NoContent();
        }

        // DELETE: api/Visits/5  — disabled by design
        [HttpDelete("{id}")]
        public IActionResult DeleteVisit(int id)
        {
            _logger.LogWarning("Direct visit deletion attempted. VisitId={VisitId}", id);
            return StatusCode(StatusCodes.Status405MethodNotAllowed,
                "Direct visit deletion is disabled. End visits using the visit workflow.");
        }
    }
}
