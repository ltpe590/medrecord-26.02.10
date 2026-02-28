using Core.DTOs;
using Core.Interfaces.Repositories;
using Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DrugCatalogsController : ControllerBase
    {
        private readonly IDrugCatalogRepository _repo;

        public DrugCatalogsController(IDrugCatalogRepository repo) => _repo = repo;

        // GET: api/DrugCatalogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DrugCatalog>>> GetDrugCatalogs() =>
            await _repo.GetAllAsync();

        // GET: api/DrugCatalogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<DrugCatalog>> GetDrugCatalog(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            return entity == null ? NotFound() : entity;
        }

        // PUT: api/DrugCatalogs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDrugCatalog(int id, DrugCatalogDto dto)
        {
            if (!await _repo.ExistsAsync(id)) return NotFound();

            // Build a disconnected entity from the DTO — EF Update() attaches it as Modified.
            var entity = new DrugCatalog
            {
                DrugId        = id,
                BrandName     = dto.BrandName,
                Composition   = dto.Composition,
                Form          = dto.Form,
                DosageStrength = dto.DosageStrength,
                Frequency     = dto.Frequency,
                Route         = dto.Route,
                Instructions  = dto.Instructions
            };

            _repo.Update(entity);
            await _repo.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/DrugCatalogs
        [HttpPost]
        public async Task<ActionResult<DrugCatalog>> PostDrugCatalog(DrugCreateDto dto)
        {
            var entity = new DrugCatalog
            {
                BrandName     = dto.BrandName,
                Composition   = dto.Composition,
                Form          = dto.Form,
                DosageStrength = dto.DosageStrength
            };

            _repo.Add(entity);
            await _repo.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDrugCatalog), new { id = entity.DrugId }, entity);
        }

        // DELETE: api/DrugCatalogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDrugCatalog(int id)
        {
            var entity = await _repo.GetByIdAsync(id);
            if (entity == null) return NotFound();

            _repo.Remove(entity);
            await _repo.SaveChangesAsync();
            return NoContent();
        }
    }
}
