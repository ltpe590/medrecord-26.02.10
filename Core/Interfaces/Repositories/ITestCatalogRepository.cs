using Core.Models;

namespace Core.Interfaces.Repositories
{
    public interface ITestCatalogRepository
    {
        Task<TestsCatalog?> GetByIdAsync(int testId);
        Task<List<TestsCatalog>> GetAllAsync();
        Task AddAsync(TestsCatalog testCatalog);
        Task UpdateAsync(TestsCatalog testCatalog);
        Task<bool> DeleteAsync(int testId);
    }
}
