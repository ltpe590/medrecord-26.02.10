using Core.Data.Context;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public class TestCatalogRepository : ITestCatalogRepository
    {
        private readonly ApplicationDbContext _context;

        public TestCatalogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TestsCatalog?> GetByIdAsync(int testId)
        {
            var testCatalog = await _context.TestCatalogs
                .FirstOrDefaultAsync(t => t.TestId == testId)
                .ConfigureAwait(false);

            return testCatalog;
        }

        public async Task<List<TestsCatalog>> GetAllAsync()
        {
            return await _context.TestCatalogs.ToListAsync();
        }

        public async Task AddAsync(TestsCatalog testCatalog)
        {
            await _context.TestCatalogs.AddAsync(testCatalog);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TestsCatalog testCatalog)
        {
            _context.TestCatalogs.Update(testCatalog);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int testId)
        {
            var test = await GetByIdAsync(testId);
            if (test == null) return false;

            _context.TestCatalogs.Remove(test);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}