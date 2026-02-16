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

        [HttpPut("{id}")]
        public async Task<IActionResult> PutPatient(int id, Patient patient)
        {
            if (id != patient.PatientId)
            {
                return BadRequest();
            }

            if (!await _patientRepository.ExistsAsync(id))
            {
                return NotFound();
            }

            await _patientRepository.UpdateAsync(patient);
            return NoContent();
        }

        // POST: api/Patients
        [HttpPost]
        public async Task<ActionResult<Patient>> PostPatient(PatientCreateDto dto)
        {
            // Note: Validation is done in dto

            // Delegate the creation logic to the mapping service
            var patient = _patientMappingService.MapToDomain(dto);

            await _patientRepository.AddAsync(patient);
            return CreatedAtAction(nameof(GetPatient), new { id = patient.PatientId }, patient);
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