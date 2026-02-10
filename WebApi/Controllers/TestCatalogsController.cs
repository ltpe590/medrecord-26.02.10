using Core.Data.Context;
using Core.DTOs;
using Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestCatalogsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestCatalogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TestCatalogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TestCatalogDto>>> GetTestCatalogs()
        {
            return await _context.TestCatalogs
                .Select(t => new TestCatalogDto
                {
                    TestId = t.TestId,
                    TestName = t.TestName,
                    TestUnit = t.TestUnit,
                    NormalRange = t.NormalRange
                })
                .ToListAsync();
        }

        // GET: api/TestCatalogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TestsCatalog>> GetTestCatalog(int id)
        {
            var testCatalog = await _context.TestCatalogs.FindAsync(id);

            if (testCatalog == null)
            {
                return NotFound();
            }

            return testCatalog;
        }

        // PUT: api/TestCatalogs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTestCatalog(int id, TestsCatalog testCatalog)
        {
            if (id != testCatalog.TestId)
            {
                return BadRequest();
            }

            _context.Entry(testCatalog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TestCatalogExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/TestCatalogs
        [HttpPost]
        public async Task<ActionResult<TestCatalogDto>> PostTestCatalog(TestCatalogCreateDto dto)
        {
            var entity = new TestsCatalog
            {
                TestName = dto.TestName,
                TestUnit = dto.TestUnit ?? string.Empty,
                NormalRange = dto.NormalRange ?? string.Empty
            };

            _context.TestCatalogs.Add(entity);
            await _context.SaveChangesAsync();

            var result = new TestCatalogDto
            {
                TestId = entity.TestId,
                TestName = entity.TestName,
                TestUnit = entity.TestUnit,
                NormalRange = entity.NormalRange
            };

            return CreatedAtAction(nameof(GetTestCatalog), new { id = result.TestId }, result);
        }

        // DELETE: api/TestCatalogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTestCatalog(int id)
        {
            var testCatalog = await _context.TestCatalogs.FindAsync(id);
            if (testCatalog == null)
            {
                return NotFound();
            }

            _context.TestCatalogs.Remove(testCatalog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TestCatalogExists(int id)
        {
            return _context.TestCatalogs.Any(e => e.TestId == id);
        }
    }
}
