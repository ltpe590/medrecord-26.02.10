using Core.DTOs;
using WPF.ViewModels;

namespace WPF.Mappers
{
    public static class PatientMapper
    {
        public static PatientViewModel ToViewModel(PatientDto dto)
        {
            if (dto == null) return null!; // caller should avoid passing null, but keep safe
            return new PatientViewModel
            {
                PatientId = dto.PatientId,
                Name = dto.Name,
                Sex = dto.Sex,
                DateOfBirth = dto.DateOfBirth,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address
            };
        }

        public static List<PatientViewModel> ToViewModels(IEnumerable<PatientDto>? dtos)
        {
            if (dtos == null) return new List<PatientViewModel>();
            return dtos.Select(ToViewModel).ToList();
        }
    }
}