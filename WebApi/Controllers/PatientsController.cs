using Core.DTOs;
using Core.Entities;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IPatientMappingService _patientMappingService;

        public PatientsController(IPatientRepository patientRepository, IPatientMappingService patientMappingService)
        {
            _patientRepository = patientRepository;
            _patientMappingService = patientMappingService;
        }

        // GET: api/Patients
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PatientDto>>> GetPatients()
        {
            var patients = await _patientRepository.GetAllAsync();
            // Use the mapping service to convert each domain model to a DTO
            var patientDtos = patients.Select(p => _patientMappingService.MapToDto(p));
            return Ok(patientDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDto>> GetPatient(int id)
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null) return NotFound();
            // Use the mapping service
            return _patientMappingService.MapToDto(patient);
        }

        // PUT: api/Patients/
        [HttpPut]
        public async Task<IActionResult> UpdatePatient([FromBody] PatientUpdateDto dto)
        {
            if (!await _patientRepository.ExistsAsync(dto.PatientId))
                return NotFound();

            var patient = await _patientRepository.GetByIdAsync(dto.PatientId);

            // Delegate the updating logic to the mapping service
            _patientMappingService.MapUpdateDtoToDomain(dto, patient);
            await _patientRepository.UpdateAsync(patient);
            return NoContent();
        }

        // NOTE: Raw-entity PUT(id, Patient) removed — it bypassed all validation.

        // POST: api/Patients
        [HttpPost]
        public async Task<ActionResult<PatientDto>> PostPatient(PatientCreateDto dto)
        {
            // Duplicate guard: same name + same DOB (or same name when DOB is absent)
            var existing = await _patientRepository.GetAllAsync();
            var duplicate = existing.Any(p =>
                string.Equals(p.Name, dto.Name, StringComparison.OrdinalIgnoreCase) &&
                (!dto.DateOfBirth.HasValue || p.DateOfBirth == dto.DateOfBirth.Value));

            if (duplicate)
                return Conflict($"A patient named '{dto.Name}'{(dto.DateOfBirth.HasValue ? $" born {dto.DateOfBirth}" : "")} already exists.");

            var patient = _patientMappingService.MapToDomain(dto);
            await _patientRepository.AddAsync(patient);
            var patientDto = _patientMappingService.MapToDto(patient);
            return CreatedAtAction(nameof(GetPatient), new { id = patientDto.PatientId }, patientDto);
        }

        // DELETE: api/Patients
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePatient(int id)
        {
            if (!await _patientRepository.ExistsAsync(id))
                return NotFound();

            await _patientRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}