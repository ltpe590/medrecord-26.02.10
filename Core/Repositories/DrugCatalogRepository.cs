using Core.Data.Context;
using Core.Interfaces.Repositories;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public sealed class DrugCatalogRepository : IDrugCatalogRepository
    {
        private readonly ApplicationDbContext _ctx;
        public DrugCatalogRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<List<DrugCatalog>> GetAllAsync() =>
            _ctx.DrugCatalogs.AsNoTracking().ToListAsync();

        public Task<DrugCatalog?> GetByIdAsync(int id) =>
            _ctx.DrugCatalogs.FindAsync(id).AsTask();

        public void Add(DrugCatalog entity)    => _ctx.DrugCatalogs.Add(entity);
        public void Update(DrugCatalog entity) => _ctx.DrugCatalogs.Update(entity);
        public void Remove(DrugCatalog entity) => _ctx.DrugCatalogs.Remove(entity);

        public Task<bool> ExistsAsync(int id) =>
            _ctx.DrugCatalogs.AnyAsync(e => e.DrugId == id);

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}
