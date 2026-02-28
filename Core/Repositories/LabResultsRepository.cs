using Core.Data.Context;
using Core.DTOs;
using Core.Interfaces.Repositories;
using Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public sealed class LabResultsRepository : ILabResultsRepository
    {
        private readonly ApplicationDbContext _ctx;
        public LabResultsRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<List<LabResultsDto>> GetByVisitAsync(int visitId) =>
            _ctx.LabResults
                .Where(r => r.VisitId == visitId)
                .Include(r => r.TestCatalog)
                .Select(r => new LabResultsDto
                {
                    LabId       = r.LabId,
                    TestId      = r.TestId,
                    VisitId     = r.VisitId,
                    ResultValue = r.ResultValue,
                    Unit        = r.Unit,
                    NormalRange = r.NormalRange,
                    Notes       = r.Notes,
                    CreatedAt   = r.CreatedAt,
                    TestName    = r.TestCatalog.TestName
                })
                .ToListAsync();

        public void Add(LabResults entity) => _ctx.LabResults.Add(entity);

        public async Task DeleteByVisitAsync(int visitId)
        {
            var rows = await _ctx.LabResults
                .Where(r => r.VisitId == visitId)
                .ToListAsync();
            _ctx.LabResults.RemoveRange(rows);
        }

        public Task SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}
