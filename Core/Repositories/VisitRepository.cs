using Core.Data.Context;
using Core.Entities;
using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Core.Repositories
{
    public sealed class VisitRepository : IVisitRepository
    {
        private readonly ApplicationDbContext _ctx;

        public VisitRepository(ApplicationDbContext ctx) => _ctx = ctx;

        public Task<Visit?> GetWithDetailsAsync(int visitId) =>
            _ctx.Visits
                .Include(v => v.Entries)
                .SingleOrDefaultAsync(v => v.VisitId == visitId);
    }
}