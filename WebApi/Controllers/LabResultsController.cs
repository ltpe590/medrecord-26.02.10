using Core.DTOs;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LabResultsController : ControllerBase
    {
        private readonly ILabResultsRepository    _labRepo;
        private readonly ITestCatalogRepository   _testRepo;
        private readonly IVisitRepository         _visitRepo;
        private readonly ILabResultsMappingService _mapper;

        public LabResultsController(
            ILabResultsRepository    labRepo,
            ITestCatalogRepository   testRepo,
            IVisitRepository         visitRepo,
            ILabResultsMappingService mapper)
        {
            _labRepo  = labRepo;
            _testRepo = testRepo;
            _visitRepo = visitRepo;
            _mapper   = mapper;
        }

        // GET: api/LabResults/visit/5
        [HttpGet("visit/{visitId}")]
        public async Task<ActionResult<IEnumerable<LabResultsDto>>> GetByVisit(int visitId) =>
            await _labRepo.GetByVisitAsync(visitId);

        // POST: api/LabResults
        [HttpPost]
        public async Task<ActionResult<LabResultsDto>> PostLabResult(LabResultCreateDto dto)
        {
            if (!await _visitRepo.ExistsAsync(dto.VisitId))
                return NotFound($"Visit {dto.VisitId} not found.");

            var test = await _testRepo.GetByIdAsync(dto.TestId);
            if (test == null)
                return NotFound($"Test {dto.TestId} not found in catalog.");

            var entity = _mapper.MapFromCreate(dto, test);

            _labRepo.Add(entity);
            await _labRepo.SaveChangesAsync();

            return CreatedAtAction(nameof(GetByVisit), new { visitId = entity.VisitId },
                _mapper.MapToDto(entity));
        }

        // DELETE: api/LabResults/visit/5
        [HttpDelete("visit/{visitId}")]
        public async Task<IActionResult> DeleteByVisit(int visitId)
        {
            await _labRepo.DeleteByVisitAsync(visitId);
            await _labRepo.SaveChangesAsync();
            return NoContent();
        }
    }
}
