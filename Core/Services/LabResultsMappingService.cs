using Core.Interfaces.Repositories;
using Core.DTOs;
using Core.Interfaces.Services;
using Core.Models;

namespace Core.Services
{
    public class LabResultsMappingService : ILabResultsMappingService
    {
        private readonly ITestCatalogRepository _testCatalogRepository;

        public LabResultsMappingService(ITestCatalogRepository testCatalogRepository)
        {
            _testCatalogRepository = testCatalogRepository;
        }

        public LabResultsDto MapToDto(LabResults domainModel)
        {
            return new LabResultsDto
            {
                LabId       = domainModel.LabId,
                TestId      = domainModel.TestId,
                VisitId     = domainModel.VisitId,
                ResultValue = domainModel.ResultValue,
                Unit        = domainModel.Unit,
                NormalRange = domainModel.NormalRange,
                Notes       = domainModel.Notes,
                CreatedAt   = domainModel.CreatedAt,
                TestName    = domainModel.TestCatalog?.TestName ?? string.Empty
            };
        }

        public async Task<LabResults> MapToDomain(LabResultsDto dto)
        {
            var testCatalog = await _testCatalogRepository.GetByIdAsync(dto.TestId)
                              ?? throw new Exception($"TestCatalog {dto.TestId} not found");

            return new LabResults
            {
                LabId       = dto.LabId,
                TestId      = dto.TestId,
                VisitId     = dto.VisitId,
                ResultValue = dto.ResultValue,
                Unit        = dto.Unit,
                NormalRange = dto.NormalRange,
                Notes       = dto.Notes,
                CreatedAt   = dto.CreatedAt,
                TestCatalog = testCatalog
            };
        }

        /// <inheritdoc/>
        public LabResults MapFromCreate(LabResultCreateDto dto, TestsCatalog testCatalog)
        {
            return new LabResults
            {
                TestId      = dto.TestId,
                VisitId     = dto.VisitId,
                ResultValue = dto.ResultValue,
                Unit        = dto.Unit,
                NormalRange = dto.NormalRange,
                Notes       = dto.Notes,
                TestCatalog = testCatalog
            };
        }
    }
}
