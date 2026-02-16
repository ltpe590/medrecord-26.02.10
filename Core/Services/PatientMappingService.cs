using Core.DTOs;
using Core.Entities;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.ValueObjects;

namespace Core.Services
{
    public class PatientMappingService : IPatientMappingService
    {
        public PatientDto MapToDto(Patient domainModel)
        {
            return new PatientDto
            {
                PatientId = domainModel.PatientId,
                Name = domainModel.Name,
                Sex = domainModel.Sex,  // No conversion needed - enum to enum
                DateOfBirth = domainModel.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                PhoneNumber = domainModel.PhoneNumber?.Value,
                BloodGroup = domainModel.BloodGroup,
                Allergies = domainModel.Allergies,
                Address = domainModel.Address,
                ShortNote = domainModel.ShortNote
            };
        }

        private string GetSexString(Sex sex)
        {
            switch (sex)
            {
                case Sex.Male:
                    return "Male";

                case Sex.Female:
                    return "Female";

                default:
                    return "Unknown";
            }
        }

        public Patient MapToDomain(PatientCreateDto dto)
        {
            PhoneNumber? phone = null;
            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                phone = new PhoneNumber(dto.PhoneNumber);

            DateOnly dob =
                dto.DateOfBirth ??
                (dto.Age.HasValue
                    ? AgeCalculator.ToDateOfBirth(dto.Age.Value)
                    : AgeCalculator.ToDateOfBirth(30));

            Sex sex = dto.Sex;  // Direct assignment - no parsing needed

            var patient = new Patient(dto.Name, sex, dob);

            patient.UpdateContact(phone, dto.Address);
            patient.UpdateClinicalInfo(dto.BloodGroup, dto.Allergies, dto.ShortNote);

            return patient;
        }

        // This new method handles updating an existing entity from the DTO
        public void MapUpdateDtoToDomain(PatientUpdateDto dto, Patient patient)
        {
            if (dto.Name != null || dto.Sex != null || dto.DateOfBirth != null)
            {
                Sex sex = dto.Sex ?? patient.Sex;  // Use nullable enum directly
                patient.UpdateIdentity(
                    dto.Name ?? patient.Name,
                    sex,
                    dto.DateOfBirth ?? patient.DateOfBirth
                );
            }

            if (dto.PhoneNumber != null || dto.Address != null)
            {
                patient.UpdateContact(
                    dto.PhoneNumber != null ? new PhoneNumber(dto.PhoneNumber) : patient.PhoneNumber,
                    dto.Address ?? patient.Address
                );
            }

            if (dto.BloodGroup != null || dto.Allergies != null || dto.ShortNote != null)
            {
                patient.UpdateClinicalInfo(
                    dto.BloodGroup ?? patient.BloodGroup,
                    dto.Allergies ?? patient.Allergies,
                    dto.ShortNote ?? patient.ShortNote
                );
            }
        }
    }
}