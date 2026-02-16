namespace WebApi.Controllers
{
    /*
    [Route("api/[controller]")]
    [ApiController]
    public class PrescriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PrescriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Prescriptions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Prescription>>> GetPrescriptions()
        {
            return await _context.Prescriptions.ToListAsync();
        }

        // GET: api/Prescriptions/ByPrescriptionId
        [HttpGet("{id}")]
        public async Task<ActionResult<Prescription>> GetPrescription(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);

            if (prescription == null)
            {
                return NotFound();
            }

            return prescription;
        }
        // GET: api/Prescriptions/ByVisitId
        [HttpGet("visit/{visitId}")]
        public async Task<ActionResult<IEnumerable<PrescriptionDto>>> GetPrescriptionsByVisit(int visitId)
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.DrugCatalog)
                .Where(p => p.VisitId == visitId)
                .Select(p => new PrescriptionDto
                {
                    PrescriptionId = p.PrescriptionId,
                    DrugId = p.DrugId,
                    DrugName = p.DrugCatalog.BrandName,
                    Dosage = p.Dosage ?? string.Empty,
                    DurationDays = p.DurationDays ?? string.Empty,
                    Form = p.DrugCatalog.Form
                })
                .ToListAsync();

            return Ok(prescriptions);
        }

        // PUT: api/Prescriptions/ByPrescriptionId
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPrescription(int id, Prescription prescription)
        {
            if (id != prescription.PrescriptionId)
            {
                return BadRequest();
            }

            _context.Entry(prescription).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PrescriptionExists(id))
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

        // POST: api/Prescriptions/ByVisitId
        [HttpPost]
        public async Task<ActionResult<Prescription>> PostPrescription(Prescription prescription)
        {
            // Optional: Basic check to ensure the associated visit exists before saving
            var visitExists = await _context.Visits.AnyAsync(v => v.VisitId == prescription.VisitId);
            if (!visitExists && prescription.VisitId != 0)
            {
                return BadRequest("The specified VisitId does not exist.");
            }

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            // Returns HTTP 201 Created
            return CreatedAtAction("GetPrescription", new { id = prescription.PrescriptionId }, prescription);
        }

        // DELETE: api/Prescriptions/ByPrescriptionId
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrescription(int id)
        {
            var prescription = await _context.Prescriptions.FindAsync(id);
            if (prescription == null)
            {
                return NotFound();
            }

            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();

            return NoContent(); // HTTP 204 No Content
        }

        // DELETE: api/Prescriptions/ByVisitId
        [HttpDelete("visit/{visitId}")]
        public async Task<IActionResult> DeletePrescriptionsByVisit(int visitId)
        {
            var prescriptions = await _context.Prescriptions
                .Where(p => p.VisitId == visitId)
                .ToListAsync();

            if (!prescriptions.Any())
            {
                return NotFound("No prescriptions found for this visit");
            }

            _context.Prescriptions.RemoveRange(prescriptions);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Helper method to check if a prescription exists
        private bool PrescriptionExists(int id)
        {
            return _context.Prescriptions.Any(e => e.PrescriptionId == id);
        }
    }
    */
}