using Core.Models;

namespace Core.Interfaces.Repositories
{
    public interface IDrugCatalogRepository
    {
        Task<List<DrugCatalog>> GetAllAsync();
        Task<DrugCatalog?> GetByIdAsync(int id);
        void Add(DrugCatalog entity);
        void Update(DrugCatalog entity);
        void Remove(DrugCatalog entity);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}
