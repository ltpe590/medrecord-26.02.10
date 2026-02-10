using Core.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Core.Entities;
using Core.Data.Context;

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
