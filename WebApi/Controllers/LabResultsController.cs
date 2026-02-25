using Core.Data.Context;
using Core.DTOs;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LabResultsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LabResultsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/LabResults/visit/5
        [HttpGet("visit/{visitId}")]
        public async Task<ActionResult<IEnumerable<LabResultsDto>>> GetByVisit(int visitId)
        {
            return await _context.LabResults
                .Where(r => r.VisitId == visitId)
                .Include(r => r.TestCatalog)
                .Select(r => new LabResultsDto
                {
                    LabId       = r.LabId,
                    TestId      = r.TestId,
                    VisitId     = r.VisitId,
                    ResultValue = r.ResultValue,
                    Unit        = r.Unit,
                    NormalRange = r.NormalRange,
                    Notes       = r.Notes,
                    CreatedAt   = r.CreatedAt,
                    TestName    = r.TestCatalog.TestName
                })
                .ToListAsync();
        }

        // POST: api/LabResults
        [HttpPost]
        public async Task<ActionResult<LabResultsDto>> PostLabResult(LabResultCreateDto dto)
        {
            // Verify visit exists
            if (!await _context.Visits.AnyAsync(v => v.VisitId == dto.VisitId))
                return NotFound($"Visit {dto.VisitId} not found.");

            // Verify test exists
            var test = await _context.TestCatalogs.FindAsync(dto.TestId);
            if (test == null)
                return NotFound($"Test {dto.TestId} not found in catalog.");

            var entity = new LabResults
            {
                TestId      = dto.TestId,
                VisitId     = dto.VisitId,
                ResultValue = dto.ResultValue,
                Unit        = dto.Unit,
                NormalRange = dto.NormalRange,
                Notes       = dto.Notes,
                TestCatalog = test
            };

            _context.LabResults.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByVisit), new { visitId = entity.VisitId },
                new LabResultsDto
                {
                    LabId       = entity.LabId,
                    TestId      = entity.TestId,
                    VisitId     = entity.VisitId,
                    ResultValue = entity.ResultValue,
                    Unit        = entity.Unit,
                    NormalRange = entity.NormalRange,
                    Notes       = entity.Notes,
                    CreatedAt   = entity.CreatedAt,
                    TestName    = test.TestName
                });
        }

        // DELETE: api/LabResults/visit/5  (clear all results for a visit before re-saving)
        [HttpDelete("visit/{visitId}")]
        public async Task<IActionResult> DeleteByVisit(int visitId)
        {
            var rows = await _context.LabResults
                .Where(r => r.VisitId == visitId)
                .ToListAsync();
            _context.LabResults.RemoveRange(rows);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
