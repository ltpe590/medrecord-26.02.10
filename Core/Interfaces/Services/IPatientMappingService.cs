using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IPatientMappingService
    {
        PatientDto MapToDto(Patient domainModel);

        Patient MapToDomain(PatientCreateDto dto);

        // Add a method for handling updates
        void MapUpdateDtoToDomain(PatientUpdateDto dto, Patient domainModel);
    }
}