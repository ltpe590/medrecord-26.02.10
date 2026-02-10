using Core.DTOs;
using Core.Models;

namespace Core.Interfaces.Services
{
    public interface ILabResultsMappingService
    {
        LabResultsDto MapToDto(LabResults domainModel);
        Task<LabResults> MapToDomain(LabResultsDto dto);
    }
}
