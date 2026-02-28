using Core.DTOs;
using Core.Interfaces.Repositories;
using Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestCatalogsController : ControllerBase
    {
        private readonly ITestCatalogRepository _repo;

        public TestCatalogsController(ITestCatalogRepository repo) => _repo = repo;

        // GET: api/TestCatalogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestCatalogDto>>> GetTestCatalogs()
        {
            var all = await _repo.GetAllAsync();
            return all.Select(t => new TestCatalogDto
            {
                TestId              = t.TestId,
                TestName            = t.TestName,
                TestUnit            = t.TestUnit,
                NormalRange         = t.NormalRange,
                UnitImperial        = t.UnitImperial        ?? string.Empty,
                NormalRangeImperial = t.NormalRangeImperial ?? string.Empty
            }).ToList();
        }

        // GET: api/TestCatalogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TestsCatalog>> GetTestCatalog(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? NotFound() : entity;
        }

        // PUT: api/TestCatalogs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTestCatalog(int id, TestsCatalog testCatalog)
        {
            if (id != testCatalog.TestId) return BadRequest();
            await _repo.UpdateAsync(testCatalog);
            return NoContent();
        }

        // POST: api/TestCatalogs
        [HttpPost]
        public async Task<ActionResult<TestCatalogDto>> PostTestCatalog(TestCatalogCreateDto dto)
        {
            var entity = new TestsCatalog
            {
                TestName            = dto.TestName,
                TestUnit            = dto.TestUnit            ?? string.Empty,
                NormalRange         = dto.NormalRange         ?? string.Empty,
                UnitImperial        = dto.UnitImperial,
                NormalRangeImperial = dto.NormalRangeImperial
            };

            await _repo.AddAsync(entity);

            return CreatedAtAction(nameof(GetTestCatalog), new { id = entity.TestId }, new TestCatalogDto
            {
                TestId              = entity.TestId,
                TestName            = entity.TestName,
                TestUnit            = entity.TestUnit,
                NormalRange         = entity.NormalRange,
                UnitImperial        = entity.UnitImperial        ?? string.Empty,
                NormalRangeImperial = entity.NormalRangeImperial ?? string.Empty
            });
        }

        // DELETE: api/TestCatalogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCatalog(int id)
        {
            var deleted = await _repo.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}
