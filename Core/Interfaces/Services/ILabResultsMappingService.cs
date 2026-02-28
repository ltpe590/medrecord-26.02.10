using Core.DTOs;
using Core.Models;

namespace Core.Interfaces.Services
{
    public interface ILabResultsMappingService
    {
        LabResultsDto MapToDto(LabResults domainModel);

        Task<LabResults> MapToDomain(LabResultsDto dto);

        /// <summary>
        /// Maps a create-request DTO + an already-fetched <see cref="TestsCatalog"/> entry
        /// to a new <see cref="LabResults"/> domain object ready for persistence.
        /// The caller is responsible for fetching and validating the catalog entry.
        /// </summary>
        LabResults MapFromCreate(LabResultCreateDto dto, TestsCatalog testCatalog);
    }
}
