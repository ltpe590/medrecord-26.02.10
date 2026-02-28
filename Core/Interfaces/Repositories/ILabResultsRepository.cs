using Core.DTOs;
using Core.Models;

namespace Core.Interfaces.Repositories
{
    public interface ILabResultsRepository
    {
        Task<List<LabResultsDto>> GetByVisitAsync(int visitId);
        void Add(LabResults entity);
        Task DeleteByVisitAsync(int visitId);
        Task SaveChangesAsync();
    }
}
